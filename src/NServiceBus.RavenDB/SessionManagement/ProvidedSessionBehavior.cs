namespace NServiceBus.RavenDB.SessionManagement
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;
    using Raven.Client;

    class ProvidedSessionBehavior : IBehavior<IncomingContext>
    {
        public Func<IDocumentSession> GetSession { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            context.Set(GetSession);
            next();
        }

        public class Registration : RegisterBehavior
        {
            public Registration()
                : base("ProvidedRavenDbSession", typeof(ProvidedSessionBehavior), "Makes sure that there is a RavenDB IDocumentSession available on the pipeline")
            {
                InsertAfter(WellKnownBehavior.UnitOfWork);
                InsertBeforeIfExists(WellKnownBehavior.InvokeSaga);
            }
        }
    }
}
