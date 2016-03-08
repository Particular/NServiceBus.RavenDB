namespace NServiceBus.RavenDB.Shutdown
{
    using System;
    using System.Collections.Generic;

    interface IContainShutdownDelegates
    {
        IEnumerable<Action> GetDelegates();
    }
}