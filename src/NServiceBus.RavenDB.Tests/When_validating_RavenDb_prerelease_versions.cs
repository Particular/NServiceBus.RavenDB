namespace NServiceBus.RavenDB.Tests
{
    using System;
    using NServiceBus.Persistence.RavenDB;
    using NUnit.Framework;

    public class When_validating_RavenDb_prerelease_versions
    {
        [TestCase("1.0.0-nightly", "2.0.0")]
        [TestCase("1.0.0-nightly", "1.1.0")]
        [TestCase("1.0.0-nightly", "1.0.1")]
        public void Server_version_is_less_than_minimum_required_version(string serverVersion, string requiredVersion)
        {
            Assert.Throws<Exception>(() => MinimumRequiredRavenDbVersion.Validate(serverVersion, requiredVersion));
        }

        [TestCase("1.0.0-nightly", "1.0.0")]
        [TestCase("1.0.0-custom", "1.0.0")]
        [TestCase("1.0.0-alpha", "1.0.0")]
        public void Server_version_equals_minimum_required_version(string serverVersion, string requiredVersion)
        {
            Assert.Throws<Exception>(() => MinimumRequiredRavenDbVersion.Validate(serverVersion, requiredVersion));
        }

        [TestCase("2.0.0-nightly", "1.0.0")]
        [TestCase("1.1.0-nightly", "1.0.0")]
        [TestCase("1.0.1-nightly", "1.0.0")]
        public void Server_version_is_greater_than_the_minimum_required_version(string serverVersion, string requiredVersion)
        {
            Assert.DoesNotThrow(() => MinimumRequiredRavenDbVersion.Validate(serverVersion, requiredVersion));
        }
    }
}