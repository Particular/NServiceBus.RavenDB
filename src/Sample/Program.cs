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
        using (var documentStore = new DocumentStore().Initialize())
        {
            var configure = Configure.With();
            configure.UsePersistence<RavenDB>(_ => _.SetDefaultDocumentStore(documentStore));
                configure.UseSerialization<Json>();
                configure.EnableFeature<Sagas>();

            configure.EnableInstallers();
            bus = configure.CreateBus();
            bus.Start();

            bus.SendLocal(new MyMessage
                {
                    SomeId = Guid.NewGuid()
                });
            Console.ReadLine();
        }
    }
}