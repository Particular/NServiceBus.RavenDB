﻿namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NServiceBus.Serialization;
    using NUnit.Framework;

    public class When_registering_custom_serializer : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_register_via_type()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointViaType>(b => b.Given(
                    (bus, c) =>
                    {
                        bus.SendLocal(new MyRequest());
                        return Task.FromResult(0);
                    }))
                .Done(c => c.HandlerGotTheRequest)
                .Run();

            Assert.IsTrue(context.SerializeCalled);
            Assert.IsTrue(context.DeserializeCalled);
        }

        [Test]
        public async Task Should_register_via_definition()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointViaDefinition>(b => b.Given(
                    (bus, c) =>
                    {
                        bus.SendLocal(new MyRequest());
                        return Task.FromResult(0);
                    }))
                .Done(c => c.HandlerGotTheRequest)
                .Run();

            Assert.IsTrue(context.SerializeCalled);
            Assert.IsTrue(context.DeserializeCalled);
        }

        public class Context : ScenarioContext
        {
            public bool HandlerGotTheRequest { get; set; }
            public bool SerializeCalled { get; set; }
            public bool DeserializeCalled { get; set; }
        }

        public class EndpointViaType : EndpointConfigurationBuilder
        {
            public EndpointViaType()
            {
                EndpointSetup<DefaultServer>(c => c.UseSerialization(typeof(MyCustomSerializer)));
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Context Context { get; set; }

                public void Handle(MyRequest request)
                {
                    Context.HandlerGotTheRequest = true;
                }
            }
        }

        public class EndpointViaDefinition : EndpointConfigurationBuilder
        {
            public EndpointViaDefinition()
            {
                EndpointSetup<DefaultServer>(c => c.UseSerialization(typeof(MySuperSerializer)));
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Context Context { get; set; }

                public void Handle(MyRequest request)
                {
                    Context.HandlerGotTheRequest = true;
                }
            }
        }

        [Serializable]
        public class MyRequest : IMessage
        {
        }

        class MySuperSerializer : SerializationDefinition
        {
            protected override Type ProvidedByFeature()
            {
                return typeof(MySuperSerializerFeature);
            }
        }

        class MySuperSerializerFeature : ConfigureSerialization
        {
            public MySuperSerializerFeature()
            {
                EnableByDefault();
            }

            protected override Type GetSerializerType(FeatureConfigurationContext context)
            {
                return typeof(MyCustomSerializer);
            }
        }

        class MyCustomSerializer : IMessageSerializer
        {
            public Context Context { get; set; }

            public void Serialize(object message, Stream stream)
            {
                var serializer = new BinaryFormatter();
                serializer.Serialize(stream, message);

                Context.SerializeCalled = true;
            }

            public object[] Deserialize(Stream stream, IList<Type> messageTypes = null)
            {
                var serializer = new BinaryFormatter();

                Context.DeserializeCalled = true;
                stream.Position = 0;
                var msg = serializer.Deserialize(stream);

                return new[]
                {
                    msg
                };
            }

            public string ContentType
            {
                get { return "MyCustomSerializer"; }
            }
        }
    }
}
