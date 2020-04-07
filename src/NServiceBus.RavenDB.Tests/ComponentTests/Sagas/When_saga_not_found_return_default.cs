namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_saga_not_found_return_default : SagaPersisterTests<SimpleSagaEntitySaga, SimpleSagaEntity>
    {
        [Test]
        public void Should_throw_when_using_finding_saga_with_property()
        {
            Assert.That(async () => await GetByCorrelationProperty("propertyNotFound", "someValue"), Throws.InstanceOf<Exception>());
        }

        [Test]
        public async Task Should_return_default_when_using_finding_saga_with_id()
        {
            var result = await GetById(Guid.Empty);
            Assert.IsNull(result);
        }

        public class AnotherSimpleSagaEntity : ContainSagaData
        {
        }
    }
}