using System;
using NServiceBus;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Persistence.SagaPersister;
using NUnit.Framework;
using Particular.Approvals;

[TestFixture]
public class SagaApprovals
{
    [Test]
    public void ApproveSagaSchema()
    {
        // if the schema is changed, the schema version must be incremented
        Approver.Verify(new SagaDataContainer
        {
            Data = new SagaData(),
            Id = nameof(SagaDataContainer.Id),
            IdentityDocId = nameof(SagaDataContainer.IdentityDocId),
        });
    }

    [Test]
    public void ApproveSagaUniqueIdentitySchema()
    {
        // if the schema is changed, the schema version must be incremented
        Approver.Verify(new SagaUniqueIdentity
        {
            UniqueValue = nameof(SagaUniqueIdentity.UniqueValue),
            Id = nameof(SagaUniqueIdentity.Id),
            SagaId = new Guid("753AB986-9699-4D06-9BE5-FF923063D19D"),
            SagaDocId = nameof(SagaUniqueIdentity.SagaDocId),
        });
    }

    class SagaData : ContainSagaData
    {
    }
}
