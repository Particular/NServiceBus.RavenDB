namespace NServiceBus.RavenDB.SessionManagement
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.RavenDB.Internal;
    using NServiceBus.RavenDB.Persistence;
    using Raven.Client;

    class OpenSessionBehavior : Behavior<PhysicalMessageProcessingContext>
    {
        readonly IDocumentStoreWrapper documentStoreWrapper;

        public static Func<IDictionary<string, string>, string> GetDatabaseName = context => string.Empty;


        public OpenSessionBehavior(IDocumentStoreWrapper documentStoreWrapper)
        {
            this.documentStoreWrapper = documentStoreWrapper;
        }

        public override async Task Invoke(PhysicalMessageProcessingContext context, Func<Task> next)
        {
            using (var session = OpenSession(context))
            {
                context.Set(session);
                await next().ConfigureAwait(false);
                session.SaveChanges();
            }
        }

        IDocumentSession OpenSession(PhysicalMessageProcessingContext context)
        {
            var databaseName = GetDatabaseName(context.Message.Headers);
            var documentSession = string.IsNullOrEmpty(databaseName) ? documentStoreWrapper.DocumentStore.OpenSession() : documentStoreWrapper.DocumentStore.OpenSession(databaseName);
            documentSession.Advanced.AllowNonAuthoritativeInformation = false;
            documentSession.Advanced.UseOptimisticConcurrency = true;
            return documentSession;
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("OpenRavenDbSession", typeof(OpenSessionBehavior), "Makes sure that there is a RavenDB IDocumentSession available on the pipeline")
            {
                InsertAfter(WellKnownStep.ExecuteUnitOfWork);
            }
        }
    }

    class RavenSessionProvider : ISessionProvider
    {
        readonly BehaviorContext context;

        public RavenSessionProvider(BehaviorContext context)
        {
            this.context = context;
        }

        public IDocumentSession Session => context.Get<IDocumentSession>();
    }
}