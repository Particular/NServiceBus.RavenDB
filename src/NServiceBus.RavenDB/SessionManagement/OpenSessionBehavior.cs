namespace NServiceBus.RavenDB.SessionManagement
{
    using System;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.RavenDB.Persistence;
    using Raven.Client;

    class OpenSessionBehavior : IBehavior<IncomingContext>
    {
        private IOpenRavenSessionsInPipeline sessionCreator;

        public OpenSessionBehavior(IOpenRavenSessionsInPipeline sessionCreator)
        {
            this.sessionCreator = sessionCreator;
        }

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
            var documentSession = sessionCreator.OpenSession(context.PhysicalMessage.Headers);
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