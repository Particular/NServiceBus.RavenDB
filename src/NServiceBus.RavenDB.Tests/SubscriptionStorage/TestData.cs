using System;
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
public class MessageA
{
}
public class MessageB
{
}

public class MessageTypes
{
    public static MessageType MessageA = new MessageType(typeof(MessageA).FullName, new Version(1, 0, 0, 0));
    public static MessageType MessageAv2 = new MessageType(typeof(MessageA).FullName, new Version(2, 0, 0, 0));
    public static MessageType MessageAv11 = new MessageType(typeof(MessageA).FullName, new Version(1, 1, 0, 0));

    public static MessageType MessageB = new MessageType(typeof(MessageB));
}

public class TestClients
{
    public static Subscriber ClientA = new Subscriber("ClientA","ClientA");
    public static Subscriber ClientB = new Subscriber("ClientB","ClientB");
    public static Subscriber ClientC = new Subscriber("ClientC", "ClientC");
    public static Subscriber ClientD = new Subscriber("ClientD", "ClientD");
}