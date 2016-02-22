namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Raven.Abstractions.Data;
    using Raven.Client;

    /// <summary>
    /// Methods for extending use of IDocumentSession for async purposes
    /// </summary>
    public static class DocumentSessionExtensions
    {
        /// <summary>
        /// Temporary method to allow easy conversion between use of IDocumentSession and IAsyncDocumentSession
        /// </summary>
        public static Task SaveChangesAsync(this IDocumentSession session)
        {
            session.SaveChanges();
            return Task.FromResult(0);
        }

        /// <summary>
        /// Temporary method to allow easy conversion between use of IDocumentSession and IAsyncDocumentSession
        /// </summary>
        public static Task<T> LoadAsync<T>(this IDocumentSession session, Guid id)
        {
            var result = session.Load<T>(id);
            return Task.FromResult(result);
        }

        /// <summary>
        /// Temporary method to allow easy conversion between use of IDocumentSession and IAsyncDocumentSession
        /// </summary>
        public static Task<T> LoadAsync<T>(this IDocumentSession session, string id)
        {
            var result = session.Load<T>(id);
            return Task.FromResult(result);
        }

        /// <summary>
        /// Temporary method to allow easy conversion between use of IDocumentSession and IAsyncDocumentSession
        /// </summary>
        public static Task<T[]> LoadAsync<T>(this IDocumentSession session, IEnumerable<string> ids)
        {
            var result = session.Load<T>(ids);
            return Task.FromResult(result);
        }

        /// <summary>
        /// Temporary method to allow easy conversion between use of IDocumentSession and IAsyncDocumentSession
        /// </summary>
        public static Task StoreAsync(this IDocumentSession session, object toStore)
        {
            session.Store(toStore);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Temporary method to allow easy conversion between use of IDocumentSession and IAsyncDocumentSession
        /// </summary>
        public static Task StoreAsync(this IDocumentSession session, object toStore, string id)
        {
            session.Store(toStore, id: id);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Temporary method to allow easy conversion between use of IDocumentSession and IAsyncDocumentSession
        /// </summary>
        public static Task StoreAsync(this IDocumentSession session, object toStore, string id, Etag etag)
        {
            session.Store(toStore, id: id, etag: etag);
            return Task.FromResult(0);
        }
    }
}