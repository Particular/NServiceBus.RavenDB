namespace NServiceBus.RavenDB.SessionManagement
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using Raven.Client;

    class ProvidedAsyncSessionBehavior : Behavior<PhysicalMessageProcessingContext>
    {

        public Func<IAsyncDocumentSession> GetAsyncSession { get; set; }

        public override Task Invoke(PhysicalMessageProcessingContext context, Func<Task> next)
        {
            context.Set(GetAsyncSession);
            return next();
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("ProvidedRavenDbAsyncSession", typeof(ProvidedAsyncSessionBehavior), "Makes sure that there is a RavenDB IAsyncDocumentSession available on the pipeline")
            {
                InsertAfter(WellKnownStep.ExecuteUnitOfWork);
                InsertBeforeIfExists(WellKnownStep.InvokeSaga);
            }
        }
    }
}