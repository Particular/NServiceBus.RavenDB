namespace NServiceBus.RavenDB.SessionManagement
{
    using System;
    using Internal;
    using Persistence;
    using Pipeline;
    using Pipeline.Contexts;
    using Raven.Client;
    using Unicast;

    class OpenSessionBehavior : IBehavior<IncomingContext>
    {
        public IDocumentStoreWrapper DocumentStoreWrapper { get; set; }

        public static Func<IMessageContext, string> GetDatabaseName = context => String.Empty;

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

        public class Registration : RegisterBehavior
        {
            public Registration()
                : base("OpenRavenDbSession", typeof(OpenSessionBehavior), "Makes sure that there is a RavenDB IDocumentSession available on the pipeline")
            {
                InsertAfter(WellKnownBehavior.ExecuteUnitOfWork);
                InsertBeforeIfExists(WellKnownBehavior.InvokeSaga);
            }
        }
    }

    class RavenSessionProvider : ISessionProvider
    {
        public PipelineExecutor PipelineExecutor { get; set; }

        public IDocumentSession Session { get { return PipelineExecutor.CurrentContext.Get<IDocumentSession>(); } }
    }
}
