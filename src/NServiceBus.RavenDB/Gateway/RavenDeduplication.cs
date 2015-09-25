﻿namespace NServiceBus.RavenDB.Gateway.Deduplication
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Gateway.Deduplication;
    using Raven.Abstractions.Exceptions;
    using Raven.Client;

    class RavenDeduplication : IDeduplicateMessages
    {
        public IDocumentStore DocumentStore { get; set; }

        public async Task<bool> DeduplicateMessage(string messageId, DateTime timeReceived)
        {
            using (var session = DocumentStore.OpenAsyncSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                session.Advanced.AllowNonAuthoritativeInformation = false;

                await session.StoreAsync(new GatewayMessage
                {
                    Id = EscapeMessageId(messageId),
                    TimeReceived = timeReceived
                });

                try
                {
                    await session.SaveChangesAsync();
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
    }
}