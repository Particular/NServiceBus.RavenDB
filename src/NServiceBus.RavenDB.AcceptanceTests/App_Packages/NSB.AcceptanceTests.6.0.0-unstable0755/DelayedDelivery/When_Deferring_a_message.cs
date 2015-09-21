﻿namespace NServiceBus.AcceptanceTests.DelayedDelivery
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_Deferring_a_message : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Delivery_should_be_delayed()
        {
            var delay = TimeSpan.FromSeconds(5);

            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.Given((bus, c) =>
                    {
                        var options = new SendOptions();

                        options.DelayDeliveryWith(delay);
                        options.RouteToLocalEndpointInstance();

                        c.SentAt = DateTime.UtcNow;

                        bus.Send(new MyMessage(), options);
                        return Task.FromResult(0);
                    }))
                    .Done(c => c.WasCalled)
                    .Run();

            Assert.GreaterOrEqual(context.ReceivedAt - context.SentAt, delay);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public DateTime SentAt { get; set; }
            public DateTime ReceivedAt { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>());
            }
            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public void Handle(MyMessage message)
                {
                    Context.ReceivedAt = DateTime.UtcNow;
                    Context.WasCalled = true;
                }
            }
        }

        [Serializable]
        public class MyMessage : IMessage
        {
        }
    }
}
