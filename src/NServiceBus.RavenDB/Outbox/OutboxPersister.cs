namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.RavenDB.Outbox;
    using NServiceBus.Transport;
    using Raven.Client;
    using Raven.Client.Documents.Commands.Batches;
    using Raven.Client.Documents.Operations;
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

        public async Task<OutboxMessage> Get(string messageId, ContextBag options)
        {
            OutboxRecord result;
            using (var session = GetSession(options))
            {
                // We use Load operation and not queries to avoid stale results
                var outboxDocId = GetOutboxRecordId(messageId);
                result = await session.LoadAsync<OutboxRecord>(outboxDocId).ConfigureAwait(false);
            }

            if (result == null)
            {
                return default;
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

            // TODO: Do we need this? Isn't this done by the session creator anyway?
            if (useClusterWideTx == false)
            {
                session.Advanced.UseOptimisticConcurrency = true;
            };

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
                session.Advanced.ClusterTransaction.CreateCompareExchangeValue($"{OutboxPersisterCompareExchangePrefix}/{outboxRecordId}", outboxRecordId);
            }

            session.StoreSchemaVersionInMetadata(outboxRecord);
        }

        public async Task SetAsDispatched(string messageId, ContextBag options)
        {
            using (var session = GetSession(options))
            {
                if (useClusterWideTx)
                {
                    // cannot use PATCH with cluster wide transactions
                    // TODO: load and save the document and update CEV? Or maybe we can delete the CEV at this point
                }
                else
                {
                    // to avoid loading the whole document we directly patch the document atomically
                    session.Advanced.Defer(new PatchCommandData(
                        id: GetOutboxRecordId(messageId),
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
                                "Expire", new { Should = timeToKeepDeduplicationData != Timeout.InfiniteTimeSpan, At = DateTime.UtcNow.Add(timeToKeepDeduplicationData) }
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

        const string OutboxPersisterCompareExchangePrefix = "outbox/transactions";
        string endpointName;
        TransportOperation[] emptyTransportOperations = new TransportOperation[0];
        IOpenTenantAwareRavenSessions sessionCreator;
        TimeSpan timeToKeepDeduplicationData;
        readonly bool useClusterWideTx;
    }
}