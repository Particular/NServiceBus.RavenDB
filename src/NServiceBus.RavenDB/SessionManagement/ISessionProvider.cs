namespace NServiceBus.RavenDB.Persistence
{
    /// <summary>
    ///     Provides access the the session managed by NServiceBus
    /// </summary>
    [ObsoleteEx( Message = "Use the 'IAsyncSessionProvider' interface.", RemoveInVersion = "5", TreatAsErrorFromVersion = "4" )]
    public interface ISessionProvider
    {
        
    }
}