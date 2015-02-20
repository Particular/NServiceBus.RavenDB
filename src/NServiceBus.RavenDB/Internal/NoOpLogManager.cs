namespace NServiceBus.RavenDB
{
    using System;
    using Raven.Abstractions.Extensions;
    using Raven.Abstractions.Logging;

    class NoOpLogManager : ILogManager
    {
        public ILog GetLogger(string name)
        {
            return new LogManager.NoOpLogger();
        }

        public IDisposable OpenNestedConext(string message)
        {
            return new DisposableAction(() => { });
        }

        public IDisposable OpenMappedContext(string key, string value)
        {
            return new DisposableAction(() => { });
        }
    }
}