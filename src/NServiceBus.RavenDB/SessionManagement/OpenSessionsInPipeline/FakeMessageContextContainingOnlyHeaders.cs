namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;

    class FakeMessageContextContainingOnlyHeaders : IMessageContext
    {
        public FakeMessageContextContainingOnlyHeaders(IDictionary<string, string> headers)
        {
            this.Headers = headers;
        }

        public string Id
        {
            get
            {
                throw new NotImplementedException("FakeMessageContextContainingOnlyHeaders does not provide access to Message Id");
            }
        }

        public Address ReplyToAddress
        {
            get
            {
                throw new NotImplementedException("FakeMessageContextContainingOnlyHeaders does not provide access to ReplyToAddress");
            }
        }

        public IDictionary<string, string> Headers { get; }
    }
}