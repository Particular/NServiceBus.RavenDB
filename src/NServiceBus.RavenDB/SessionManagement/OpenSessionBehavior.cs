namespace NServiceBus.RavenDB.SessionManagement
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.RavenDB.Internal;
    using NServiceBus.RavenDB.Persistence;
    using NServiceBus.Unicast;
    using Raven.Client;

    class OpenSessionBehavior : PhysicalMessageProcessingStageBehavior
    {
        readonly IDocumentStoreWrapper documentStoreWrapper;

        public static Func<IMessageContext, string> GetDatabaseName = context => String.Empty;


        public OpenSessionBehavior(IDocumentStoreWrapper documentStoreWrapper)
        {
            this.documentStoreWrapper = documentStoreWrapper;
        }

        public override void Invoke(Context context, Action next)
        {
            using (var session = OpenSession(context))
            {
                context.Set(session);
                next();
                session.SaveChanges();
            }
        }

        IDocumentSession OpenSession(Context context)
        {
            var databaseName = GetDatabaseName(new MessageContext(context.GetPhysicalMessage()));
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
                InsertBeforeIfExists(WellKnownStep.InvokeSaga);
                InsertAfterIfExists("OutboxDeduplication");
                InsertBeforeIfExists("OutboxRecorder");
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

        public IDocumentSession Session { get { return context.Get<IDocumentSession>(); } }
    }
}