using System;
using System.Collections.Generic;
using NServiceBus.RavenDB.Outbox;
using NUnit.Framework;
using Particular.Approvals;

[TestFixture]
public class OutboxAprovals
{
    [Test]
    public void ApproveOutboxSchema()
    {
        // if the schema is changed, the schema version must be incremented
        Approver.Verify(new OutboxRecord
        {
            MessageId = nameof(OutboxRecord.MessageId),
            Dispatched = true,
            DispatchedAt = new DateTime(2020, 05, 06, 10, 10, 10, 10),
            TransportOperations = new[]
            {
                new OutboxRecord.OutboxOperation
                {
                    MessageId = nameof(OutboxRecord.OutboxOperation.MessageId),
                    Message = Array.Empty<byte>(),
                    Headers = new Dictionary<string, string>(),
                    Options = new Dictionary<string, string>(),
                },
            },
        });
    }
}
