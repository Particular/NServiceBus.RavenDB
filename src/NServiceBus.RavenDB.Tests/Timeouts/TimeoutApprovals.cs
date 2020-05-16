using System;
using System.Collections.Generic;
using NUnit.Framework;
using Particular.Approvals;
using Timeout = NServiceBus.TimeoutPersisters.RavenDB.TimeoutData;

[TestFixture]
public class TimeoutApprovals
{
    [Test]
    public void ApproveTimeoutSchema()
    {
        // if the schema is changed make sure to increase the schema version
        Approver.Verify(new Timeout
        {
            Id = nameof(Timeout.Id),
            Destination = nameof(Timeout.Destination),
            SagaId = new Guid("05F4B926-FD16-486F-8440-7ED42BA2C4DF"),
            State = Array.Empty<byte>(),
            Time = new DateTime(2020, 05, 06, 10, 10, 10, 10),
            OwningTimeoutManager = nameof(Timeout.OwningTimeoutManager),
            Headers = new Dictionary<string, string>()
        });
    }
}