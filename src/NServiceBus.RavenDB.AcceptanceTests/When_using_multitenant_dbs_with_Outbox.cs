namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Features;
    using NServiceBus.Persistence;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.RavenDB.Persistence;
    using NServiceBus.Saga;
    using NUnit.Framework;
    using Raven.Client.Document;

    public class When_using_multitenant_dbs_with_Outbox : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_honor_SetMessageToDatabaseMappingConvention()
        {
            RunTest(cfg =>
            {
                string dbName;
                cfg.PersistenceExtensions.SetMessageToDatabaseMappingConvention(msgContext => msgContext.Headers.TryGetValue("RavenDatabaseName", out dbName) ? dbName : cfg.DefaultStore.DefaultDatabase);
            });
        }

        [Test]
        public void Should_honor_UseSharedAsyncSession()
        {
            RunTest(cfg =>
            {
                cfg.PersistenceExtensions.UseSharedSession(headers => cfg.DefaultStore.OpenSession(headers["RavenDatabaseName"]));
            });
        }

        void RunTest(Action<ContextDbConfig> configureMultiTenant)
        {
            var context = new Context
            {
                Db1 = "Tenant1-" + Guid.NewGuid().ToString("n").Substring(16),
                Db2 = "Tenant2-" + Guid.NewGuid().ToString("n").Substring(16)
            };

            var oldValue = 0;

            Scenario.Define(context)
                .WithEndpoint<MultiTenantEndpoint>(b =>
                {
                    b.CustomConfig(cfg =>
                    {
                        cfg.EnableOutbox();

                        cfg.Pipeline.Register<MessageCountingBehavior.Register>();

                        var settings = cfg.GetSettings();

                        var defaultStore = (DocumentStore)ConfigureRavenDBPersistence.GetDefaultDocumentStore(settings);
                        context.DefaultDb = defaultStore.DefaultDatabase;
                        context.DbConfig.DefaultStore = defaultStore;

                        ConfigureRavenDBPersistence.CreateDocumentStore(context.Db1).Initialize();
                        ConfigureRavenDBPersistence.CreateDocumentStore(context.Db2).Initialize();

                        context.DbConfig.PersistenceExtensions = ConfigureRavenDBPersistence.GetDefaultPersistenceExtensions(settings);
                        configureMultiTenant(context.DbConfig);
                    });

                    b.When(bus =>
                    {
                        var msgId1 = Guid.NewGuid().ToString();
                        var msgId2 = Guid.NewGuid().ToString();

                        SendMessage(bus, msgId1, "OrderA", context.Db1);
                        SendMessage(bus, msgId1, "OrderA", context.Db1);
                        SendMessage(bus, msgId2, "OrderB", context.Db2);
                        SendMessage(bus, msgId2, "OrderB", context.Db2);
                    });
                })
                .Done(c => //c.MessagesObserved >= 4)
                {
                    var newValue = c.MessagesObserved;
                    if (newValue != oldValue)
                    {
                        var debug = $"Old {oldValue}, New {newValue}";
                        Console.WriteLine(debug);
                        oldValue = newValue;
                    }

                    return newValue >= 4;
                })
                .Run();

            // Acceptance tests in these versions don't currently clean databases until the end
            // ConfigureRavenDBPersistence.DeleteDatabase(context.Db1);
            // ConfigureRavenDBPersistence.DeleteDatabase(context.Db2);

            Assert.AreEqual(4, context.MessagesObserved);
            Assert.AreEqual(2, context.ObservedDbs.Count);
            Assert.IsFalse(context.ObservedDbs.Any(db => db == context.DefaultDb));
            Assert.Contains(context.Db1, context.ObservedDbs);
            Assert.Contains(context.Db2, context.ObservedDbs);
        }

        private void SendMessage(IBus bus, string messageId, string orderId, string dbName)
        {
            var msg = new TestMsg {OrderId = orderId};
            bus.SetMessageHeader(msg, "RavenDatabaseName", dbName);
            bus.SetMessageHeader(msg, Headers.MessageId, messageId);
            bus.SendLocal(msg);
        }

        public class Context : ScenarioContext
        {
            public string DefaultDb { get; set; }
            public string Db1 { get; set; }
            public string Db2 { get; set; }
            public List<string> ObservedDbs { get; } = new List<string>();
            public string ObservedDbsOutput => String.Join(", ", ObservedDbs);
            public ContextDbConfig DbConfig { get; } = new ContextDbConfig();
            public int MessagesObserved;
        }

        public class ContextDbConfig
        {
            public DocumentStore DefaultStore { get; set; }
            public DocumentStore Tenant1 { get; set; }
            public DocumentStore Tenant2 { get; set; }
            public PersistenceExtentions<RavenDBPersistence> PersistenceExtensions { get; set; }
        }

        public class MultiTenantEndpoint : EndpointConfigurationBuilder
        {
            public MultiTenantEndpoint()
            {
                EndpointSetup<DefaultServer>(customize =>
                {
                    customize.DisableFeature<TimeoutManager>();
                });
            }

            public class MTSaga : Saga<MTSagaData>,
                IAmStartedByMessages<TestMsg>
            {
                Context testCtx;
                ISessionProvider sessionProvider;

                public MTSaga(Context testCtx, ISessionProvider sessionProvider)
                {
                    this.testCtx = testCtx;
                    this.sessionProvider = sessionProvider;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MTSagaData> mapper)
                {
                    mapper.ConfigureMapping<TestMsg>(m => m.OrderId).ToSaga(s => s.OrderId);
                }

                public void Handle(TestMsg message)
                {
                    Data.OrderId = message.OrderId;

                    var ravenSession = sessionProvider.Session;
                    if (ravenSession is InMemoryDocumentSessionOperations)
                    {
                        var ravenSessionOps = ravenSession as InMemoryDocumentSessionOperations;
                        var dbName = ravenSessionOps.DatabaseName;
                        testCtx.ObservedDbs.Add(dbName);
                    }
                }
            }

            public class MTSagaData : ContainSagaData
            {
                [Unique]
                public virtual string OrderId { get; set; }
            }

            public class ConcurrencySettings : IProvideConfiguration<TransportConfig>
            {
                public TransportConfig GetConfiguration()
                {
                    return new TransportConfig
                    {
                        MaximumConcurrencyLevel = 1
                    };
                }
            }
        }

        public class TestMsg : ICommand
        {
            public string OrderId { get; set; }
        }

        public class MessageCountingBehavior : IBehavior<IncomingContext>
        {
            Context testContext;
            private static object padlock = new object();

            public MessageCountingBehavior(Context testContext)
            {
                this.testContext = testContext;
            }

            public void Invoke(IncomingContext context, Action next)
            {
                next();

                lock (padlock)
                {
                    testContext.MessagesObserved++;
                    var debug = $"{testContext.MessagesObserved} observed, {testContext.ObservedDbs.Count} dbs observed";
                    Console.WriteLine(debug);
                }
            }

            public class Register : RegisterStep
            {
                public Register() : base("MessageCountingBehavior", typeof(MessageCountingBehavior), "Counts all messages that finish the pipeline")
                {
                    InsertBefore(WellKnownStep.CreateChildContainer);
                }
            }
        }
    }
}