
namespace NServiceBus.RavenDB.Tests
{
    using System;
    using NuGet.Versioning;
    using NUnit.Framework;

    public class When_validating_RavenDb_versions
    {
        [Test]
        public void Major_versions_not_meeting_minimum_requirements_throws()
        {
            var ravenDbVersion = "0.0.0";

            var minimumRequiredVersion = new NuGetVersion("1.0.0");

            Assert.Throws<Exception>(() => RavenDbVersionValidator.ValidateVersion(ravenDbVersion, minimumRequiredVersion));
        }

        [Test]
        public void Major_versions_meeting_minimum_requirements_does_not_throws()
        {
            var ravenDbVersion = "1.0.0";

            var minimumRequiredVersion = new NuGetVersion("1.0.0");

            Assert.DoesNotThrow(() => RavenDbVersionValidator.ValidateVersion(ravenDbVersion, minimumRequiredVersion));
        }

        [Test]
        public void Minor_versions_meeting_minimum_requirements_does_not_throw()
        {
            var ravenDbVersion = "1.1.0";

            var minimumRequiredVersion = new NuGetVersion("1.1.0");

            Assert.DoesNotThrow(() => RavenDbVersionValidator.ValidateVersion(ravenDbVersion, minimumRequiredVersion));
        }

        [Test]
        public void Minor_versions_not_meeting_minimum_requirements_throws()
        {
            var ravenDbVersion = "1.0.0";

            var minimumRequiredVersion = new NuGetVersion("1.1.0");

            Assert.Throws<Exception>(() => RavenDbVersionValidator.ValidateVersion(ravenDbVersion, minimumRequiredVersion));
        }

        [Test]
        public void Minor_versions_meeting_minimum_requirements_with_major_version_not_meeting_minimum_requirement_throws()
        {
            var ravenDbVersion = "0.1.0";

            var minimumRequiredVersion = new NuGetVersion("1.1.0");

            Assert.Throws<Exception>(() => RavenDbVersionValidator.ValidateVersion(ravenDbVersion, minimumRequiredVersion));
        }

        [Test]
        public void Patch_versions_meeting_minimum_requirements_does_not_throw()
        {
            var ravenDbVersion = "1.1.0";

            var minimumRequiredVersion = new NuGetVersion("1.1.0");

            Assert.DoesNotThrow(() => RavenDbVersionValidator.ValidateVersion(ravenDbVersion, minimumRequiredVersion));
        }

        [Test]
        public void Patch_versions_not_meeting_minimum_requirements_throws()
        {
            var ravenDbVersion = "1.0.0";

            var minimumRequiredVersion = new NuGetVersion("1.0.1");

            Assert.Throws<Exception>(() => RavenDbVersionValidator.ValidateVersion(ravenDbVersion, minimumRequiredVersion));
        }

        [Test]
        public void Prerelease_versions_that_exceed_the_minimum_required_version_do_not_throw()
        {
            var nightlyVersion = "1.0.1-nightly";

            var requiredVersion = new NuGetVersion("1.0.0");

            Assert.DoesNotThrow(() => RavenDbVersionValidator.ValidateVersion(nightlyVersion, requiredVersion));
        }

        [TestCase("1.0.0-alpha", "1.0.0")]
        [TestCase("1.0.0-nightly", "1.0.0")]
        [TestCase("1.0.0-custom", "1.0.0")]
        [TestCase("1.0.0-alpha", "1.0.0-beta")]
        public void Prerelease_versions_that_do_not_exceed_minimum_required_version_throws(string ravenDbVersion, string minimumVersionString)
        {
            //pre-release version numbers should be treated as versions less than the released version
            //Example: 1.0.0-prerelease is an earlier version than 1.0.0 

            var minimumVersion = new NuGetVersion(minimumVersionString);

            Assert.Throws<Exception>(() => RavenDbVersionValidator.ValidateVersion(ravenDbVersion, minimumVersion));
        }
    }
}