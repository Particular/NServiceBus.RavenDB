namespace NServiceBus.RavenDB.Tests
{
    using NServiceBus.Extensibility;
    using Raven.Client.Documents.Session;

    static class AsyncDocumentSessionExtensions
    {
        public static IAsyncDocumentSession UsingOptimisticConcurrency(this IAsyncDocumentSession session)
        {
            session.Advanced.UseOptimisticConcurrency = true;
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
