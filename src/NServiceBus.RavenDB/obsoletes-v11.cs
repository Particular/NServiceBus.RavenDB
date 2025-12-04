#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace NServiceBus;

using System;
using Particular.Obsoletes;

public partial class RavenDBPersistence
{
    [ObsoleteMetadata(
        Message = "The RavenDBPersistence class is not supposed to be instantiated directly",
        RemoveInVersion = "12",
        TreatAsErrorFromVersion = "11")]
    [Obsolete("The RavenDBPersistence class is not supposed to be instantiated directly. Will be removed in version 12.0.0.", true)]
    public RavenDBPersistence() => throw new NotImplementedException();
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member