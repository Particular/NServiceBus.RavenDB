﻿namespace NServiceBus.RavenDB.Gateway.Deduplication
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Gateway.Deduplication;
    using Raven.Abstractions.Exceptions;
    using Raven.Client;

    class RavenDeduplication : IDeduplicateMessages
    {
        public RavenDeduplication(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;
        }

        public async Task<bool> DeduplicateMessage(string messageId, DateTime timeReceived, ContextBag context)
        {
            using (var session = documentStore.OpenAsyncSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                session.Advanced.AllowNonAuthoritativeInformation = false;

                await session.StoreAsync(new GatewayMessage
                {
                    Id = EscapeMessageId(messageId),
                    TimeReceived = timeReceived
                }).ConfigureAwait(false);

                try
                {
                    await session.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (ConcurrencyException)
                {
                    return false;
                }

                return true;
            }
        }

        static string EscapeMessageId(string messageId)
        {
            return messageId.Replace("\\", "_");
        }

        IDocumentStore documentStore;
    }
}