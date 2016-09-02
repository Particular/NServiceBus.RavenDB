namespace NServiceBus.RavenDB.Tests.Outbox
{
    using System;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NUnit.Framework;

    [TestFixture]
    public class OutboxApiExtensions
    {
        [Test]
        public void TestSetTimeToKeepDeduplicationData()
        {
            var cfg = new EndpointConfiguration("Test");

            var outbox = cfg.EnableOutbox();

            outbox.SetTimeToKeepDeduplicationData(TimeSpan.FromMinutes(42));

            Assert.AreEqual(42, cfg.GetSettings().Get<TimeSpan>("Outbox.TimeToKeepDeduplicationData").Minutes);
        }

        [Test]
        public void SetFrequencyToRunDeduplicationDataCleanup()
        {
            var cfg = new EndpointConfiguration("Test");

            var outbox = cfg.EnableOutbox();

            outbox.SetFrequencyToRunDeduplicationDataCleanup(TimeSpan.FromMinutes(13));

            Assert.AreEqual(13, cfg.GetSettings().Get<TimeSpan>("Outbox.FrequencyToRunDeduplicationDataCleanup").Minutes);
        }
    }
}
