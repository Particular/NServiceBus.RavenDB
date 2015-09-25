using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

public class MySaga : Saga<MySagaData>, IAmStartedByMessages<MyMessage>
{
    static ILog logger = LogManager.GetLogger(typeof(MySaga));

    public Task Handle(MyMessage message)
    {
        logger.Info("Hello from MySaga");
        return Task.FromResult(0);
    }

    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
    {
        mapper.ConfigureMapping<MyMessage>(m => m.SomeId)
            .ToSaga(s => s.SomeId);
    }
}