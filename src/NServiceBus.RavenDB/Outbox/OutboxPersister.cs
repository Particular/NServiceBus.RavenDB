﻿namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;
    using NServiceBus.RavenDB.Outbox;
    using Transport;
    using Raven.Client;
    using Raven.Client.Documents.Commands.Batches;
    using Raven.Client.Documents.Operations;
    using Raven.Client.Documents.Operations.CompareExchange;
    using Raven.Client.Documents.Session;
    using TransportOperation = Outbox.TransportOperation;

    class OutboxPersister : IOutboxStorage
    {
        public OutboxPersister(string endpointName, IOpenTenantAwareRavenSessions sessionCreator, TimeSpan timeToKeepDeduplicationData, bool useClusterWideTx)
        {
            this.endpointName = endpointName;
            this.sessionCreator = sessionCreator;
            this.timeToKeepDeduplicationData = timeToKeepDeduplicationData;
            this.useClusterWideTx = useClusterWideTx;
        }

        public async Task<OutboxMessage> Get(string messageId, ContextBag context)
        {
            OutboxRecord result;
            using (var session = GetSession(context))
            {
                // We use Load operation and not queries to avoid stale results
                var outboxRecordId = GetOutboxRecordId(messageId);
                result = await session.LoadAsync<OutboxRecord>(outboxRecordId).ConfigureAwait(false);

                if (result == null)
                {
                    return default;
                }

                var outboxRecordCev = await session.Advanced.ClusterTransaction.GetCompareExchangeValueAsync<string>($"{OutboxPersisterCompareExchangePrefix}/{outboxRecordId}").ConfigureAwait(false);
                context.Set(OutboxPersisterCompareExchangeContextKey, outboxRecordCev);
            }

            if (result.Dispatched || result.TransportOperations.Length == 0)
            {
                return new OutboxMessage(result.MessageId, emptyTransportOperations);
            }

            var transportOperations = new TransportOperation[result.TransportOperations.Length];
            var index = 0;
            foreach (var op in result.TransportOperations)
            {
                transportOperations[index] = new TransportOperation(op.MessageId, op.Options, op.Message, op.Headers);
                index++;
            }

            return new OutboxMessage(result.MessageId, transportOperations);
        }


        public Task<OutboxTransaction> BeginTransaction(ContextBag context)
        {
            var session = GetSession(context);
            var transaction = new RavenDBOutboxTransaction(session);
            return Task.FromResult<OutboxTransaction>(transaction);
        }

        public async Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
        {
            var session = ((RavenDBOutboxTransaction)transaction).AsyncSession;

            var operations = new OutboxRecord.OutboxOperation[message.TransportOperations.Length];

            var index = 0;
            foreach (var transportOperation in message.TransportOperations)
            {
                operations[index] = new OutboxRecord.OutboxOperation
                {
                    Message = transportOperation.Body,
                    Headers = transportOperation.Headers,
                    MessageId = transportOperation.MessageId,
                    Options = transportOperation.Options
                };
                index++;
            }

            var outboxRecord = new OutboxRecord
            {
                MessageId = message.MessageId,
                Dispatched = false,
                TransportOperations = operations
            };

            string outboxRecordId = GetOutboxRecordId(message.MessageId);
            await session.StoreAsync(outboxRecord, outboxRecordId).ConfigureAwait(false);

            if (useClusterWideTx)
            {
                var compareExchangeValue = context.Get<CompareExchangeValue<string>>(OutboxPersisterCompareExchangeContextKey);
                if (compareExchangeValue == null)
                {
                    session.Advanced.ClusterTransaction.CreateCompareExchangeValue($"{OutboxPersisterCompareExchangePrefix}/{outboxRecordId}", outboxRecordId);
                }
                else
                {
                    //handling concurrent processing in the case of Store succeeded by SetAsDispatched not yet.
                    session.Advanced.ClusterTransaction.UpdateCompareExchangeValue(compareExchangeValue);
                }
            }

            session.StoreSchemaVersionInMetadata(outboxRecord);
        }

        public async Task SetAsDispatched(string messageId, ContextBag context)
        {
            using (var session = GetSession(context))
            {
                var outboxRecordId = GetOutboxRecordId(messageId);
                var expireMetadataValue = new { Should = timeToKeepDeduplicationData != Timeout.InfiniteTimeSpan, At = DateTime.UtcNow.Add(timeToKeepDeduplicationData) };
                if (useClusterWideTx)
                {
                    //this is tricky
                    //if outboxRecordCev != null it is a SetAsDispatched without a Store: the outbox record in the Get op was not null.
                    //we will only update the CEV to make sure that no other SetAsDispatched can succeed.
                    var outboxRecordCev = context.Get<CompareExchangeValue<string>>(OutboxPersisterCompareExchangeContextKey);
                    if (outboxRecordCev == null)
                    {
                        //there was no CEV during get and one should have been created by Store
                        outboxRecordCev = await session.Advanced.ClusterTransaction.GetCompareExchangeValueAsync<string>($"{OutboxPersisterCompareExchangePrefix}/{outboxRecordId}").ConfigureAwait(false);
                    }

                    // cannot use PATCH with cluster wide transactions
                    var outboxRecord = await session.LoadAsync<OutboxRecord>(outboxRecordId).ConfigureAwait(false);
                    if (!outboxRecord.Dispatched)
                    {
                        outboxRecord.Dispatched = true;
                        outboxRecord.DispatchedAt = DateTime.UtcNow;
                        session.StoreSchemaVersionInMetadata(outboxRecord);

                        var metadata = session.Advanced.GetMetadataFor(outboxRecord);
                        metadata[Constants.Documents.Metadata.Expires] = expireMetadataValue;

                        //If the OutboxRecord was modified then we also have to update the CEV;
                        //otherwise no need for anything
                        session.Advanced.ClusterTransaction.UpdateCompareExchangeValue(outboxRecordCev);
                    }
                }
                else
                {
                    // to avoid loading the whole document we directly patch the document atomically
                    session.Advanced.Defer(new PatchCommandData(
                        id: outboxRecordId,
                        changeVector: null,
                        patch: new PatchRequest
                        {
                            Script =
    $@"if(this.Dispatched === true)
  return;
this.Dispatched = true
this.DispatchedAt = args.DispatchedAt.Now
this.TransportOperations = []
this['@metadata']['{SchemaVersionExtensions.OutboxRecordSchemaVersionMetadataKey}'] = args.SchemaVersion.Version
if(args.Expire.Should === false)
  return;
this['@metadata']['{Constants.Documents.Metadata.Expires}'] = args.Expire.At",
                            Values =
                            {
                            {
                                "DispatchedAt", new { Now = DateTime.UtcNow }
                            },
                            {
                                "SchemaVersion", new { Version = OutboxRecord.SchemaVersion }
                            },
                            {
                                "Expire", expireMetadataValue
                            }
                            }
                        },
                        patchIfMissing: null));
                }

                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        IAsyncDocumentSession GetSession(ContextBag context)
        {
            var message = context.Get<IncomingMessage>();

            return sessionCreator.OpenSession(message.Headers);
        }

        string GetOutboxRecordId(string messageId) => $"Outbox/{endpointName}/{messageId.Replace('\\', '_')}";

        const string OutboxPersisterCompareExchangeContextKey = "NServiceBus.RavenDB.ClusterWideTx.Outbox";
        internal const string OutboxPersisterCompareExchangePrefix = "outbox/transactions";
        string endpointName;
        TransportOperation[] emptyTransportOperations = new TransportOperation[0];
        IOpenTenantAwareRavenSessions sessionCreator;
        TimeSpan timeToKeepDeduplicationData;
        readonly bool useClusterWideTx;
    }
}