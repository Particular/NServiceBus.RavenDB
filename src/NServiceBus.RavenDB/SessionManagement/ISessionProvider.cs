namespace NServiceBus.Persistence.RavenDB
{
    /// <summary>
    ///     Provides access the the session managed by NServiceBus
    /// </summary>
    [ObsoleteEx( Message = "The session is now exposed through SynchronizedStorageSession.RavenSession(). For handlers use context.SynchronizedStorageSession.RavenSession(). For ", RemoveInVersion = "5", TreatAsErrorFromVersion = "4" )]
    public interface ISessionProvider
    {
        
    }
}