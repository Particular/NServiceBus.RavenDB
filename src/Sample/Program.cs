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
            var configure = Configure.With()
                .DefaultBuilder()
                .UsePersistence<RavenDB>(_ => _.SetDefaultDocumentStore(documentStore))
                .Serialization.Json()
                .Features(_ => _.Enable<Sagas>())
                ;

            bus = configure.CreateBus();
            bus.RunInstallers();
            bus.Start();

            bus.SendLocal(new MyMessage
                {
                    SomeId = Guid.NewGuid()
                });
            Console.ReadLine();
        }
    }
}