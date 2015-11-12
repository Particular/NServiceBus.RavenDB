namespace NServiceBus.AcceptanceTests.NonDTC
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_outbox_is_enabled
    {
        [Test]
        public void Downstream_duplicates_are_eliminated()
        {
            var context = new Context();
            Scenario.Define(context)
                .WithEndpoint<EndpointWithOutboxAndAuditOn>(b => b.Given(bus =>
                {
                    bus.OutgoingHeaders["NServiceBus.MessageId"] = Guid.NewGuid().ToString();
                    bus.SendLocal(new DuplicateMessage());
                    bus.SendLocal(new DuplicateMessage());
                    bus.OutgoingHeaders.Remove("NServiceBus.MessageId");

                    bus.SendLocal(new MarkerMessage());
                }))
                .WithEndpoint<DownstreamEndpoint>()
                .Done(c => c.Done)
                .Run();

            Assert.IsTrue(context.Done);
            Assert.AreEqual(1, context.DownstreamMessageCount);
        }

        public class Context : ScenarioContext
        {
            public int DownstreamMessageCount { get; set; }
            public bool Done { get; set; }
        }

        public class DuplicateMessage : IMessage
        {
        }

        public class MarkerMessage : IMessage
        {
        }

        public class DownstreamMessage : IMessage
        {
        }

        public class EndpointWithOutboxAndAuditOn : EndpointConfigurationBuilder
        {
            public EndpointWithOutboxAndAuditOn()
            {
                EndpointSetup<DefaultServer>(
                    b =>
                    {
                        b.GetSettings().Set("DisableOutboxTransportCheck", true);
                        b.EnableFeature<TimeoutManager>();
                        b.EnableOutbox();
                    })
                    .AddMapping<DownstreamMessage>(typeof(DownstreamEndpoint))
                    .AddMapping<MarkerMessage>(typeof(DownstreamEndpoint));
            }

            class DuplicateMessageHandler : IHandleMessages<DuplicateMessage>
            {
                public IBus Bus { get; set; }

                public void Handle(DuplicateMessage message)
                {
                    Bus.Send(new DownstreamMessage());
                }
            }

            class MarkerMessageHandler : IHandleMessages<MarkerMessage>
            {
                public IBus Bus { get; set; }

                public void Handle(MarkerMessage message)
                {
                    Bus.Send(message);
                }
            }
        }

        public class DownstreamEndpoint : EndpointConfigurationBuilder
        {
            public DownstreamEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class DownstreamMessageHandler : IHandleMessages<DownstreamMessage>
            {
                public Context Context { get; set; }

                public void Handle(DownstreamMessage message)
                {
                    Context.DownstreamMessageCount++;
                }
            }

            class MarkerMessageHandler : IHandleMessages<MarkerMessage>
            {
                public Context Context { get; set; }

                public void Handle(MarkerMessage message)
                {
                    Context.Done = true;
                }
            }
        }
    }
}