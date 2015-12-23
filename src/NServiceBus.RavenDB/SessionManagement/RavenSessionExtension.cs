using Raven.Client;

namespace NServiceBus.RavenDB.Persistence
{
    /// <summary>
    /// Extensions, to the message handler context, to manage RavenDB session.
    /// </summary>
    public static class RavenSessionExtension
    {
        /// <summary>
        /// Gets the current RavenDB session.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static IAsyncDocumentSession GetRavenSession(this IMessageHandlerContext context)
        {
            return context.Extensions.Get<IAsyncDocumentSession>();
        }
    }
}
