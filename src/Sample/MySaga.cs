using NServiceBus.Logging;
using NServiceBus.Saga;

public class MySaga:Saga<MySagaData>, IAmStartedByMessages<MyMessage>
{
    static ILog logger = LogManager.GetLogger(typeof(MySaga));

    public void Handle(MyMessage message)
    {
        logger.Info("Hello from MySaga"); 
    }

    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
    {
        mapper.ConfigureMapping<MyMessage>(m => m.SomeId)
            .ToSaga(s => s.SomeId);
    }
}