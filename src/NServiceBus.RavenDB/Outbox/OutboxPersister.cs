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
    using Raven.Client.Exceptions;
    using TransportOperation = Outbox.TransportOperation;

    class OutboxPersister : IOutboxStorage
    {
        public OutboxPersister(string endpointName, IOpenTenantAwareRavenSessions sessionCreator, TimeSpan timeToKeepDeduplicationData, bool useClusterWideTransactions)
        {
            this.endpointName = endpointName;
            this.sessionCreator = sessionCreator;
            this.timeToKeepDeduplicationData = timeToKeepDeduplicationData;
            this.useClusterWideTransactions = useClusterWideTransactions;
        }

        public async Task<OutboxMessage> Get(string messageId, ContextBag options, CancellationToken cancellationToken = default)
        {
            OutboxRecord result;
            using (var session = GetSession(options))
            {
                // We use Load operation and not queries to avoid stale results
                var outboxDocId = GetOutboxRecordId(messageId);
                result = await session.LoadAsync<OutboxRecord>(outboxDocId, cancellationToken).ConfigureAwait(false);
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
                var dispatchProperties = op.Options == null ? null : new DispatchProperties(op.Options);
                transportOperations[index] = new TransportOperation(op.MessageId, dispatchProperties, op.Message, op.Headers);
                index++;
            }

            return new OutboxMessage(result.MessageId, transportOperations);
        }


        public Task<IOutboxTransaction> BeginTransaction(ContextBag context, CancellationToken cancellationToken = default)
        {
            var session = GetSession(context);

            if (!useClusterWideTransactions)
            {
                session.Advanced.UseOptimisticConcurrency = true;
            }

            var transaction = new RavenDBOutboxTransaction(session);
            return Task.FromResult<IOutboxTransaction>(transaction);
        }

        public async Task Store(OutboxMessage message, IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            var session = ((RavenDBOutboxTransaction)transaction).AsyncSession;

            var operations = new OutboxRecord.OutboxOperation[message.TransportOperations.Length];

            var index = 0;
            foreach (var transportOperation in message.TransportOperations)
            {
                operations[index] = new OutboxRecord.OutboxOperation
                {
                    Message = transportOperation.Body.ToArray(),
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

            await session.StoreAsync(outboxRecord, GetOutboxRecordId(message.MessageId), cancellationToken).ConfigureAwait(false);
            session.StoreSchemaVersionInMetadata(outboxRecord);
        }

        public async Task SetAsDispatched(string messageId, ContextBag options, CancellationToken cancellationToken = default)
        {
            using (var session = GetSession(options))
            {
                var outboxRecordId = GetOutboxRecordId(messageId);
                if (useClusterWideTransactions)
                {
                    var outboxRecord = await session.LoadAsync<OutboxRecord>(outboxRecordId, cancellationToken).ConfigureAwait(false);

                    if (!outboxRecord.Dispatched)
                    {
                        outboxRecord.Dispatched = true;
                        outboxRecord.DispatchedAt = DateTime.UtcNow;
                        outboxRecord.TransportOperations = Array.Empty<OutboxRecord.OutboxOperation>();

                        var metadata = session.Advanced.GetMetadataFor(outboxRecord);
                        metadata[SchemaVersionExtensions.OutboxRecordSchemaVersionMetadataKey] = OutboxRecord.SchemaVersion;

                        if (timeToKeepDeduplicationData != Timeout.InfiniteTimeSpan)
                        {
                            metadata.Add(Constants.Documents.Metadata.Expires, DateTime.UtcNow.Add(timeToKeepDeduplicationData));
                        }
                    }
                }
                else
                {
                    // to avoid loading the whole document we directly patch the document atomically, this only works for single-node environments
                    session.Advanced.Defer(new PatchCommandData(
                        id: outboxRecordId,
                        changeVector: null,
                        patch: new PatchRequest
                        {
                            Script =
    $@"if(this.Dispatched === true)
      return;
    this.Dispatched = true;
    this.DispatchedAt = args.DispatchedAt.Now;
    this.TransportOperations = [];
    var metadata = this['@metadata'];
    metadata['{SchemaVersionExtensions.OutboxRecordSchemaVersionMetadataKey}'] = args.SchemaVersion.Version;
    if(args.Expire.Should === false)
      return;
    metadata['{Constants.Documents.Metadata.Expires}'] = args.Expire.At;",
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

                try
                {
                    await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (ConcurrencyException) when (useClusterWideTransactions)
                {
                    // When cluster wide transactions are enabled and two concurrent operations try to set as dispatched 
                    // it is OK to swallow the exception since the outcome is deterministic
                    // patching for non cluster wide transactions would always work and never land here
                }
            }
        }

        IAsyncDocumentSession GetSession(ContextBag context)
        {
            var message = context.Get<IncomingMessage>();

            return sessionCreator.OpenSession(message.Headers);
        }

        string GetOutboxRecordId(string messageId) => $"Outbox/{endpointName}/{messageId.Replace('\\', '_')}";

        string endpointName;
        TransportOperation[] emptyTransportOperations = new TransportOperation[0];
        IOpenTenantAwareRavenSessions sessionCreator;
        TimeSpan timeToKeepDeduplicationData;
        bool useClusterWideTransactions;
    }
}