namespace NServiceBus.RavenDB.Persistence
{
    /// <summary>
    ///     Provides access the the session managed by NServiceBus
    /// </summary>
    [ObsoleteEx( Message = "The session is now exposed through SynchronizedStorageSession.Session(). For handlers use context.SynchronizedStorageSession.Session(). For ", RemoveInVersion = "5", TreatAsErrorFromVersion = "4" )]
    public interface ISessionProvider
    {
        
    }
}