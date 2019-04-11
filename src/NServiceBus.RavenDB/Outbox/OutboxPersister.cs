namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.RavenDB.Outbox;
    using NServiceBus.Transport;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Session;
    using TransportOperation = NServiceBus.Outbox.TransportOperation;

    class OutboxPersister : IOutboxStorage
    {
        public OutboxPersister(IDocumentStore documentStore, string endpointName, IOpenRavenSessionsInPipeline sessionCreator)
        {
            this.documentStore = documentStore;
            this.endpointName = endpointName;
            this.sessionCreator = sessionCreator;
        }

        public async Task<OutboxMessage> Get(string messageId, ContextBag options)
        {
            OutboxRecord result;
            using (var session = GetSession(options))
            {
                // TODO: Ensure AllowNonAuthoritativeInformation = false not needed
                //session.Advanced.AllowNonAuthoritativeInformation = false;

                // We use Load operation and not queries to avoid stale results
                var possibleIds = GetPossibleOutboxDocumentIds(messageId);
                var docs = await session.LoadAsync<OutboxRecord>(possibleIds).ConfigureAwait(false);
                result = docs.FirstOrDefault(o => o != null);
            }

            if (result == null)
            {
                return default(OutboxMessage);
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

            session.Advanced.UseOptimisticConcurrency = true;

            context.Set(session);
            var transaction = new RavenDBOutboxTransaction(session);
            return Task.FromResult<OutboxTransaction>(transaction);
        }

        public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
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

            return session.StoreAsync(new OutboxRecord
            {
                MessageId = message.MessageId,
                Dispatched = false,
                TransportOperations = operations
            }, GetOutboxRecordId(message.MessageId));
        }

        public async Task SetAsDispatched(string messageId, ContextBag options)
        {
            using (var session = GetSession(options))
            {
                session.Advanced.UseOptimisticConcurrency = true;
                // TODO: Ensure AllowNonAuthoritativeInformation = false not needed
                //session.Advanced.AllowNonAuthoritativeInformation = false;


                var docs = await session.LoadAsync<OutboxRecord>(GetPossibleOutboxDocumentIds(messageId)).ConfigureAwait(false);
                var outboxMessage = docs.Values.FirstOrDefault(o => o != null);
                if (outboxMessage == null || outboxMessage.Dispatched)
                {
                    return;
                }

                outboxMessage.Dispatched = true;
                outboxMessage.DispatchedAt = DateTime.UtcNow;
                outboxMessage.TransportOperations = emptyOutboxOperations;

                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        IAsyncDocumentSession GetSession(ContextBag context)
        {
            IncomingMessage message;
            if (context.TryGet(out message))
            {
                return sessionCreator.OpenSession(message.Headers);
            }

            return documentStore.OpenAsyncSession();
        }

        string[] GetPossibleOutboxDocumentIds(string messageId)
        {
            return new[]
            {
                // Current format, already replaces \ with _
                GetOutboxRecordId(messageId),
                // Legacy format without endpoint id
                $"Outbox/{messageId.Replace('\\', '_')}"
            };
        }

        string GetOutboxRecordId(string messageId) => $"Outbox/{endpointName}/{messageId.Replace('\\', '_')}";

        string endpointName;
        IDocumentStore documentStore;
        TransportOperation[] emptyTransportOperations = new TransportOperation[0];
        OutboxRecord.OutboxOperation[] emptyOutboxOperations = new OutboxRecord.OutboxOperation[0];
        IOpenRavenSessionsInPipeline sessionCreator;
    }
}