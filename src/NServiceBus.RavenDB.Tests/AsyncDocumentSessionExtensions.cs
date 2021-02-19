namespace NServiceBus.RavenDB.Tests
{
    using NServiceBus.Extensibility;
    using Raven.Client.Documents.Session;

    static class AsyncDocumentSessionExtensions
    {
        public static IAsyncDocumentSession UsingOptimisticConcurrency(this IAsyncDocumentSession session, bool useClusterWideTransactions)
        {
            session.Advanced.UseOptimisticConcurrency = !useClusterWideTransactions;
            return session;
        }

        public static IAsyncDocumentSession InContext(this IAsyncDocumentSession session, out ContextBag context)
        {
            context = new ContextBag();
            context.Set(session);
            return session;
        }
    }
}
