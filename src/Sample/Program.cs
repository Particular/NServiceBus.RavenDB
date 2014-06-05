using System;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;
using Raven.Client.Document;

class Program
{
    static IStartableBus bus;

    static void Main()
    {

        Configure.Serialization.Json();

        Feature.Enable<Sagas>();
        using (var documentStore = new DocumentStore().Initialize())
        {
            var configure = Configure.With()
                .DefaultBuilder()
                .UsePersistence<RavenDB>(_ => _.SetDefaultDocumentStore(documentStore))
                ;

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