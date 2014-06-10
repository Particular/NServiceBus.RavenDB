namespace NServiceBus.RavenDB.SessionManagement
{
    using System;
    using Internal;
    using Persistence;
    using Pipeline;
    using Pipeline.Contexts;
    using Raven.Client;

    class OpenSessionBehavior : IBehavior<IncomingContext>
    {
        public IDocumentStoreWrapper DocumentStoreWrapper { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            using (var session = DocumentStoreWrapper.DocumentStore.OpenSession())
            {
                context.Set(session);
                next();
                session.SaveChanges();
            }
        }

        public class Registration : RegisterBehavior
        {
            public Registration()
                : base("OpenRavenDbSession", typeof(OpenSessionBehavior), "Makes sure that there is a RavenDB IDocumentSession available on the pipeline")
            {
                InsertAfter(WellKnownBehavior.UnitOfWork);
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
