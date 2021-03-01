namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Threading;
    using Raven.Client.Documents.Session;

    class CurrentSessionHolder
    {
        public IAsyncDocumentSession Current => pipelineContext.Value.Session;

        public void SetCurrentSession(IAsyncDocumentSession session)
        {
            pipelineContext.Value.Session = session;
        }

        public IDisposable CreateScope()
        {
            if (pipelineContext.Value != null)
            {
                throw new InvalidOperationException("Attempt to overwrite an existing session context.");
            }
            var wrapper = new Wrapper();
            pipelineContext.Value = wrapper;
            return new Scope(this);
        }

        readonly AsyncLocal<Wrapper> pipelineContext = new AsyncLocal<Wrapper>();

        class Wrapper
        {
            public IAsyncDocumentSession Session;
        }

        class Scope : IDisposable
        {
            public Scope(CurrentSessionHolder sessionHolder)
            {
                this.sessionHolder = sessionHolder;
            }

            public void Dispose()
            {
                sessionHolder.pipelineContext.Value = null;
            }

            readonly CurrentSessionHolder sessionHolder;
        }

    }
}