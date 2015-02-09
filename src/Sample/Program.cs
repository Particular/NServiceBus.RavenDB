using System;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;

class Program
{
    static void Main()
    {
        var configure = new BusConfiguration();
        configure.UsePersistence<RavenDBPersistence>();
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