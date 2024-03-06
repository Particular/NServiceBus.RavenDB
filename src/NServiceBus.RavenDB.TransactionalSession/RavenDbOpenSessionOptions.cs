namespace NServiceBus.TransactionalSession
{
    using System;
    using System.Collections.Generic;
    using Transport;

    /// <summary>
    /// The options allowing to control the behavior of the transactional session.
    /// </summary>
    public sealed class RavenDbOpenSessionOptions : OpenSessionOptions
    {
        /// <summary>
        /// Creates a new instance of the RavenDbOpenSessionOptions.
        /// </summary>
        /// <param name="multiTenantConnectionContext">The connection context when multi-tenancy is used.</param>
        public RavenDbOpenSessionOptions(IDictionary<string, string> multiTenantConnectionContext = null)
        {
            var headers = multiTenantConnectionContext != null ? new Dictionary<string, string>(multiTenantConnectionContext) : [];

            // order matters because instantiating IncomingMessage is modifying the headers
            foreach (var header in headers)
            {
                Metadata.Add(header.Key, header.Value);
            }

            Extensions.Set(new IncomingMessage(SessionId, headers, ReadOnlyMemory<byte>.Empty));
        }
    }
}