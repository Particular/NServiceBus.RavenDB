namespace NServiceBus.RavenDB.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Outbox;
    using NServiceBus.RavenDB.Persistence;
    using Raven.Client;

    class OutboxPersister : IOutboxStorage
    {
        readonly ISessionProvider sessionProvider;

        public OutboxPersister(ISessionProvider sessionProvider)
        {
            this.sessionProvider = sessionProvider;
        }

        public IDocumentStore DocumentStore { get; set; }

        public bool TryGet(string messageId, out OutboxMessage message)
        {
            OutboxRecord result;
            using (var session = DocumentStore.OpenSession())
            {
                // We use Load operation and not queries to avoid stale results
                result = session.Load<OutboxRecord>(GetOutboxRecordId(messageId));
            }

            if (result == null)
            {
                message = null;
                return false;
            }

            message = new OutboxMessage(result.MessageId);
            message.TransportOperations.AddRange(
                result.TransportOperations.Select(t => new TransportOperation(t.MessageId, t.Options, t.Message, t.Headers))
                );

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
            using (var session = DocumentStore.OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                var outboxMessage = session.Load<OutboxRecord>(GetOutboxRecordId(messageId));
                if (outboxMessage == null || outboxMessage.Dispatched)
                {
                    return;
                }

                outboxMessage.Dispatched = true;
                outboxMessage.DispatchedAt = DateTime.UtcNow;

                session.SaveChanges();
            }
        }

        static string GetOutboxRecordId(string messageId)
        {
            return "Outbox/" + messageId;
        }
    }
}