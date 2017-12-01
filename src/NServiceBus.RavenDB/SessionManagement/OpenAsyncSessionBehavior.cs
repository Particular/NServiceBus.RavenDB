namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using Raven.Client;

    class OpenAsyncSessionBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public OpenAsyncSessionBehavior(IOpenRavenSessionsInPipeline sessionCreator)
        {
            this.sessionCreator = sessionCreator;
        }

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            IAsyncDocumentSession session;
            if (context.Extensions.TryGet(out session))
            {
                // Already an active session, just proceed
                await next().ConfigureAwait(false);
            }
            else
            {
                using (session = sessionCreator.OpenSession(context.Message.Headers))
                {
                    context.Extensions.Set(session);
                    await next().ConfigureAwait(false);
                    await session.SaveChangesAsync().ConfigureAwait(false);
                }
            }
        }
        
        readonly IOpenRavenSessionsInPipeline sessionCreator;
    }
}