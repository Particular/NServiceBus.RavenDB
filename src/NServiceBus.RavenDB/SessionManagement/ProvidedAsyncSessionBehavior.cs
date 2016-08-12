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
    }
}