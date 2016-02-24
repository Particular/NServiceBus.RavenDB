namespace NServiceBus.RavenDB.SessionManagement
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using Raven.Client;

    class ProvidedAsyncSessionBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        private ReadOnlySettings settings;

        public ProvidedAsyncSessionBehavior(ReadOnlySettings settings)
        {
            this.settings = settings;
        }

        public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            Func<IAsyncDocumentSession> asyncSessionFactory;
            settings.TryGet(RavenDbSettingsExtensions.SharedAsyncSessionSettingsKey, out asyncSessionFactory);
            context.Extensions.Set(asyncSessionFactory);
            return next();
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("ProvidedRavenDbAsyncSession", typeof(ProvidedAsyncSessionBehavior), "Makes sure that there is a RavenDB IAsyncDocumentSession available on the pipeline")
            {
                InsertAfter(WellKnownStep.ExecuteUnitOfWork);
                InsertBeforeIfExists(WellKnownStep.InvokeSaga);
            }
        }
    }
}