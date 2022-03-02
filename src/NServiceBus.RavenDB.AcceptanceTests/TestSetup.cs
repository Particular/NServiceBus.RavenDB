using NServiceBus;
using NUnit.Framework;

[SetUpFixture]
public class TestSetup
{
    [OneTimeSetUp]
    public void SetUp()
    {
        // ensure the RavenDB assembly is loaded into the AppDomain because it needs its features to be scanned to work properly.
        typeof(RavenDBPersistence).ToString();
    }
}