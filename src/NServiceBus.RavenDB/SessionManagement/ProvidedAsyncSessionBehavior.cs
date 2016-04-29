namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using Raven.Client;

    class ProvidedAsyncSessionBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public ProvidedAsyncSessionBehavior(Func<IAsyncDocumentSession> getAsyncSession)
        {
            this.getAsyncSession = getAsyncSession;
        }

        public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            context.Extensions.Set(getAsyncSession);
            return next();
        }
        
        Func<IAsyncDocumentSession> getAsyncSession;

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