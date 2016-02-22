namespace NServiceBus.RavenDB.SessionManagement
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using Raven.Client;

    class ProvidedAsyncSessionBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public Func<IDocumentSession> GetAsyncSession { get; set; }

        public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            context.Extensions.Set(GetAsyncSession);
            return next();
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("ProvidedRavenDbAsyncSession", typeof(ProvidedAsyncSessionBehavior), "Makes sure that there is a RavenDB IDocumentSession available on the pipeline")
            {
                InsertAfter(WellKnownStep.ExecuteUnitOfWork);
                InsertBeforeIfExists(WellKnownStep.InvokeSaga);
            }
        }
    }
}