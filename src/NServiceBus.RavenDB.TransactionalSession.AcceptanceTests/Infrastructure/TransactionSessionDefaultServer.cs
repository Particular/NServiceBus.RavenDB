namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using NUnit.Framework;

    public class TransactionSessionDefaultServer : IEndpointSetupTemplate
    {
        public virtual async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomization,
            Func<EndpointConfiguration, Task> configurationBuilderCustomization)
        {
            var endpointConfiguration = new EndpointConfiguration(endpointCustomization.EndpointName);

            endpointConfiguration.EnableInstallers();
            endpointConfiguration.UseSerialization<SystemJsonSerializer>();
            endpointConfiguration.Recoverability()
                .Delayed(delayed => delayed.NumberOfRetries(0))
                .Immediate(immediate => immediate.NumberOfRetries(0));
            endpointConfiguration.SendFailedMessagesTo("error");

            var storageDir = Path.Combine(Path.GetTempPath(), "learn", TestContext.CurrentContext.Test.ID);

            endpointConfiguration.UseTransport(new AcceptanceTestingTransport
            {
                StorageLocation = storageDir
            });

            var persistence = endpointConfiguration.UsePersistence<RavenDBPersistence>();
            persistence.EnableTransactionalSession();
            persistence.SetDefaultDocumentStore(SetupFixture.DocumentStore);
            persistence.SetMessageToDatabaseMappingConvention(headers =>
            {
                if (headers.TryGetValue("tenant-id", out var tenantValue))
                {
                    return tenantValue;
                }

                return SetupFixture.DefaultDatabaseName;
            });

            endpointConfiguration.RegisterStartupTask(sp => new CaptureServiceProviderStartupTask(sp, runDescriptor.ScenarioContext));

            await configurationBuilderCustomization(endpointConfiguration).ConfigureAwait(false);

            // scan types at the end so that all types used by the configuration have been loaded into the AppDomain
            endpointConfiguration.TypesToIncludeInScan(endpointCustomization.GetTypesScopedByTestClass());

            return endpointConfiguration;
        }
    }
}