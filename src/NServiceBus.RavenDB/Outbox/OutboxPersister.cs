namespace NServiceBus.RavenDB.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.RavenDB.Persistence;
    using NServiceBus.RavenDB.SessionManagement;
    using Raven.Client;

    class OutboxPersister : IOutboxStorage
    {
        readonly ISessionProvider sessionProvider;
        readonly IOpenRavenSessionsInPipeline sessionCreator;

        public string EndpointName { get; set; }

        public OutboxPersister(ISessionProvider sessionProvider, IOpenRavenSessionsInPipeline sessionCreator)
        {
            this.sessionProvider = sessionProvider;
            this.sessionCreator = sessionCreator;
        }

        public IDocumentStore DocumentStore { get; set; }

        public bool TryGet(string messageId, out OutboxMessage message)
        {
            OutboxRecord result;
            using (var session = GetSession())
            {
                session.Advanced.AllowNonAuthoritativeInformation = false;
                // We use Load operation and not queries to avoid stale results
                result = session.Load<OutboxRecord>(new[] { GetOutboxRecordId(messageId), GetOutboxRecordIdWithoutEndpointName(messageId) }).FirstOrDefault(o => o != null);
            }

            if (result == null)
            {
                message = null;
                return false;
            }

            message = new OutboxMessage(result.MessageId);
            if (!result.Dispatched)
            {
                message.TransportOperations.AddRange(
                    result.TransportOperations.Select(t => new TransportOperation(t.MessageId, t.Options, t.Message, t.Headers))
                    );
            }
            return true;
        }

        public void Store(string messageId, IEnumerable<TransportOperation> transportOperations)
        {
            var session = sessionProvider.Session;
            session.Advanced.UseOptimisticConcurrency = true;

            session.Store(new OutboxRecord
            {
                MessageId = messageId,
                Dispatched = false,
                TransportOperations = transportOperations.Select(t => new OutboxRecord.OutboxOperation
                {
                    Message = t.Body,
                    Headers = t.Headers,
                    MessageId = t.MessageId,
                    Options = t.Options
                }).ToList()
            }, GetOutboxRecordId(messageId));
        }

        public void SetAsDispatched(string messageId)
        {
            using (var session = GetSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                session.Advanced.AllowNonAuthoritativeInformation = false;
                var outboxMessage = session.Load<OutboxRecord>(new[] { GetOutboxRecordId(messageId), GetOutboxRecordIdWithoutEndpointName(messageId) }).FirstOrDefault(o => o != null);

                if (outboxMessage == null || outboxMessage.Dispatched)
                {
                    return;
                }

                outboxMessage.Dispatched = true;
                outboxMessage.DispatchedAt = DateTime.UtcNow;
                outboxMessage.TransportOperations = emptyOutboxOperations;

                session.SaveChanges();
            }
        }

        IDocumentSession GetSession()
        {
            var ravenSessionProvider = sessionProvider as RavenSessionProvider;
            if (ravenSessionProvider != null)
            {
                var incomingContext = (IncomingContext) ravenSessionProvider.PipelineExecutor.CurrentContext;

                return sessionCreator.OpenSession(incomingContext.PhysicalMessage.Headers);
            }

            return DocumentStore.OpenSession();
        }

        static string GetOutboxRecordIdWithoutEndpointName(string messageId) => $"Outbox/{messageId.Replace('\\', '_')}";
        OutboxRecord.OutboxOperation[] emptyOutboxOperations = new OutboxRecord.OutboxOperation[0];

        string GetOutboxRecordId(string messageId) => $"Outbox/{EndpointName}/{messageId.Replace('\\', '_')}";
    }
}
