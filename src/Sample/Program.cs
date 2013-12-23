using System;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Installation.Environments;
using NServiceBus.RavenDB;
using Raven.Client.Document;

class Program
{
    static IStartableBus bus;

    static void Main()
    {
        LoggingConfig.ConfigureLogging();

        Configure.Serialization.Json();

        Feature.Enable<Sagas>();
        using (var documentStore = new DocumentStore())
        {
            var configure = Configure.With();
            configure.DefaultBuilder();
            configure.RavenDBStorageAsDefault(documentStore);
            bus = configure.UnicastBus().CreateBus();

            bus.Start(() => Configure.Instance.ForInstallationOn<Windows>().Install());

            bus.SendLocal(new MyMessage
                {
                    SomeId = Guid.NewGuid()
                });
            Console.ReadLine();
        }
    }
}