namespace NServiceBus.RavenDB.SessionManagement
{
    using System;
    using NServiceBus.Pipeline;
    using Raven.Client;

    class ProvidedSessionBehavior : PhysicalMessageProcessingStageBehavior
    {
        public Func<IDocumentSession> GetSession { get; set; }

        public override void Invoke(Context context, Action next)
        {
            context.Set(GetSession);
            next();
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("ProvidedRavenDbSession", typeof(ProvidedSessionBehavior), "Makes sure that there is a RavenDB IDocumentSession available on the pipeline")
            {
                InsertAfter(WellKnownStep.ExecuteUnitOfWork);
                InsertBeforeIfExists(WellKnownStep.InvokeSaga);
            }
        }
    }
}