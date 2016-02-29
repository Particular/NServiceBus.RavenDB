namespace NServiceBus.RavenDB.Shutdown
{
    using System;
    using System.Collections.Generic;

    class ShutdownDelegateRegistry : IRegisterShutdownDelegates, IContainShutdownDelegates
    {
        private List<Action> shutdownDelegates = new List<Action>();

        public void Register(Action action)
        {
            shutdownDelegates.Add(action);
        }

        public IEnumerable<Action> GetDelegates()
        {
            return shutdownDelegates;
        }
    }
}