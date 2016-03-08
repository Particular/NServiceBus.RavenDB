namespace NServiceBus.RavenDB.Shutdown
{
    using System;

    interface IRegisterShutdownDelegates
    {
        void Register(Action action);
    }
}