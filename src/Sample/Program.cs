using System;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;
using Raven.Client.Document;

class Program
{
    static void Main()
    {
        using (var documentStore = new DocumentStore().Initialize())
        {
            var configure = new BusConfiguration();
            configure.UsePersistence<RavenDBPersistence>().SetDefaultDocumentStore(documentStore);
            configure.UseSerialization<JsonSerializer>();
            configure.EnableFeature<Sagas>();
            configure.EnableInstallers();

            using (var bus = Bus.Create(configure))
            {
                bus.Start();

                bus.SendLocal(new MyMessage
                {
                    SomeId = Guid.NewGuid()
                });
            }

            Console.ReadLine();
        }
    }
}