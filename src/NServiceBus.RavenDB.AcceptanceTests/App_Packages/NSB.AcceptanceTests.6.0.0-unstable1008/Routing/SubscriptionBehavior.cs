namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    static class SubscriptionBehaviorExtensions
    {
        public static void OnEndpointSubscribed<TContext>(this BusConfiguration b, Action<SubscriptionEventArgs, TContext> action) where TContext : ScenarioContext
        {
            b.Pipeline.Register<SubscriptionBehavior<TContext>.Registration>();

            b.RegisterComponents(c => c.ConfigureComponent(builder =>
            {
                var context = builder.Build<TContext>();
                return new SubscriptionBehavior<TContext>(action, context, builder.Build<ReadOnlySettings>().EndpointName().ToString());
            }, DependencyLifecycle.InstancePerCall));
        }
    }

    class SubscriptionBehavior<TContext> : Behavior<PhysicalMessageProcessingContext> where TContext : ScenarioContext
    {
        Action<SubscriptionEventArgs, TContext> action;
        TContext scenarioContext;
        string endpoint;

        public SubscriptionBehavior(Action<SubscriptionEventArgs, TContext> action, TContext scenarioContext,string endpoint)
        {
            this.action = action;
            this.scenarioContext = scenarioContext;
            this.endpoint = endpoint;
        }

        public override async Task Invoke(PhysicalMessageProcessingContext context, Func<Task> next)
        {
            var subscriptionMessageType = GetSubscriptionMessageTypeFrom(context.Message);

            var messageId = context.Message.MessageId;
            if (subscriptionMessageType != null)
            {
                scenarioContext.AddTrace($"{endpoint}:{messageId} - About to process subscription to {subscriptionMessageType}");
            }
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                scenarioContext.AddTrace($"{endpoint}:{messageId} - Failed to process subscription {subscriptionMessageType} : {ex.ToString()}");
                throw;
            }
   
            if (subscriptionMessageType != null)
            {
                scenarioContext.AddTrace($"{endpoint}:{messageId} - Triggering subscribed event for to {subscriptionMessageType}");
                action(new SubscriptionEventArgs
                {
                    MessageType = subscriptionMessageType,
                    SubscriberReturnAddress = context.Message.GetReplyToAddress()
                }, scenarioContext);

                scenarioContext.AddTrace($"{endpoint}:{messageId} - Subscribed event for {subscriptionMessageType} completed");
            }
        }

        static string GetSubscriptionMessageTypeFrom(IncomingMessage msg)
        {
            return (from header in msg.Headers where header.Key == Headers.SubscriptionMessageType select header.Value).FirstOrDefault();
        }

        internal class Registration : RegisterStep
        {
            public Registration()
                : base("SubscriptionBehavior", typeof(SubscriptionBehavior<TContext>), "So we can get subscription events")
            {
                InsertBefore("ProcessSubscriptionRequests");
            }
        }
    }
}