using System;
using System.Collections.Generic;
using NServiceBus.Routing;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

public interface ISomeInterface
{
}
public interface ISomeInterface2
{
}
public interface ISomeInterface3
{
}

public class MessageB
{
}

public class MessageA
{

}
public class MessageTypes
{
    public static IReadOnlyCollection<MessageType> MessageA = new[] { new MessageType(typeof(MessageA).FullName, new Version(1, 0, 0, 0)) };
    public static IReadOnlyCollection<MessageType> MessageAv2 = new[] { new MessageType(typeof(MessageA).FullName, new Version(2, 0, 0, 0)) };
    public static IReadOnlyCollection<MessageType> MessageAv11 = new[] { new MessageType(typeof(MessageA).FullName, new Version(1, 1, 0, 0)) };

    public static IReadOnlyCollection<MessageType> MessageB = new[] { new MessageType(typeof(MessageB)) };

    public static IReadOnlyCollection<MessageType> All = new[] { new MessageType(typeof(MessageA)), new MessageType(typeof(MessageB)) };
}

public class TestClients
{
    public static Subscriber ClientA = new Subscriber("ClientA", new Endpoint("ClientA"));
    public static Subscriber ClientB = new Subscriber("ClientB", new Endpoint("ClientB"));
    public static Subscriber ClientC = new Subscriber("ClientC", new Endpoint("ClientC"));
}