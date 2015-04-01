namespace NServiceBus.RavenDB.SessionManagement
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using Raven.Client;

    class ProvidedSessionBehavior : Behavior<PhysicalMessageProcessingContext>
    {
        public Func<IDocumentSession> GetSession { get; set; }

        public override Task Invoke(PhysicalMessageProcessingContext context, Func<Task> next)
        {
            context.Set(GetSession);
            return next();
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("ProvidedRavenDbSession", typeof(ProvidedSessionBehavior), "Makes sure that there is a RavenDB IDocumentSession available on the pipeline")
            {
                InsertAfter(WellKnownStep.ExecuteUnitOfWork);
                InsertBeforeIfExists(WellKnownStep.InvokeSaga);
            }
        }
    }
}