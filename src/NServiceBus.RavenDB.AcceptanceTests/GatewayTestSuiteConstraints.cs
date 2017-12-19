namespace NServiceBus.Gateway.AcceptanceTests
{
    using NServiceBus.RavenDB.AcceptanceTests;

    public partial class GatewayTestSuiteConstraints :IGatewayTestSuiteConstraints
    {
        public IConfigureGatewayPersitenceExecution CreatePersistenceConfiguration()
        {
            return new ConfigureRavenDBGatewayPersitence();
        }
    }
}