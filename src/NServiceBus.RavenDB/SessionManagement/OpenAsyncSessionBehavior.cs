namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using Raven.Client.Documents.Session;

    class OpenAsyncSessionBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public OpenAsyncSessionBehavior(IOpenTenantAwareRavenSessions sessionCreator)
        {
            this.sessionCreator = sessionCreator;
        }

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            if (context.Extensions.TryGet(out IAsyncDocumentSession session))
            {
                // Already an active session from the Outbox, just proceed
                // SaveChangesAsync is called by RavenDBOutboxTransaction
                await next().ConfigureAwait(false);
            }
            else
            {
                using (session = sessionCreator.OpenSession(context.Message.Headers))
                {
                    //context.Extensions.Set(session);
                    await next().ConfigureAwait(false);
                    await session.SaveChangesAsync().ConfigureAwait(false);
                }
            }
        }

        readonly IOpenTenantAwareRavenSessions sessionCreator;
    }
}