namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using NServiceBus.ObjectBuilder;

    public interface IInjectBuilder
    {
        IBuilder Builder { get; set; }
    }
}