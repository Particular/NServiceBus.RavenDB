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

            bus.SendLocalAsync(new MyMessage
            {
                SomeId = Guid.NewGuid()
            }).GetAwaiter().GetResult();
        }

        Console.ReadLine();
    }
}