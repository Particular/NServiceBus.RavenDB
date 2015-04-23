namespace NServiceBus.RavenDB.SessionManagement
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.RavenDB.Internal;
    using NServiceBus.RavenDB.Persistence;
    using NServiceBus.Unicast;
    using Raven.Client;

    class OpenSessionBehavior : IBehavior<IncomingContext>
    {
        public static Func<IMessageContext, string> GetDatabaseName = context => String.Empty;
        public IDocumentStoreWrapper DocumentStoreWrapper { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            using (var session = OpenSession(context))
            {
                context.Set(session);
                next();
                session.SaveChanges();
            }
        }

        IDocumentSession OpenSession(IncomingContext context)
        {
            var databaseName = GetDatabaseName(new MessageContext(context.PhysicalMessage));
            var documentSession = string.IsNullOrEmpty(databaseName) ? DocumentStoreWrapper.DocumentStore.OpenSession() : DocumentStoreWrapper.DocumentStore.OpenSession(databaseName);
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
        public PipelineExecutor PipelineExecutor { get; set; }

        public IDocumentSession Session
        {
            get { return PipelineExecutor.CurrentContext.Get<IDocumentSession>(); }
        }
    }
}