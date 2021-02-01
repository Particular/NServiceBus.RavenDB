namespace NServiceBus.Persistence.RavenDB
{
    using System;

    /// <summary>
    /// The saga persistence configuration options.
    /// </summary>
    public class SagaPersistenceConfiguration
    {
        /// <summary>
        /// Enables or disables default saga persistence pessimistic locking. Default to optimistic locking when not used.
        /// </summary>
        /// <param name="value">True to enable pessimistic locking, otherwise optimistic locking.</param>
        [ObsoleteEx(Message = "Pessimistic locking will be the default in next major. Use UseOptimisticLocking() to enable optimistic locking.",
            TreatAsErrorFromVersion = "7.0",
            RemoveInVersion = "8.0")]
        public void UsePessimisticLocking(bool value = true)
        {
            EnablePessimisticLocking = value;
        }

        /// <summary>
        /// Enables default saga persistence optimistic locking. Default to pessimistic locking when not used.
        /// </summary>
        public void UseOptimisticLocking()
        {
            EnablePessimisticLocking = false;
        }

        /// <summary>
        /// Set saga persistence pessimistic lease lock duration. Default is 60 seconds.
        /// </summary>
        /// <param name="value">Pessimistic lease lock duration.</param>
        public void SetPessimisticLeaseLockTime(TimeSpan value)
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Lease lock time must be greater than zero.");
            }

            LeaseLockTime = value;
        }

        /// <summary>
        /// Set saga persistence pessimistic lease lock acquisition timeout. Default is 60 seconds.
        /// </summary>
        /// <param name="value"></param>
        public void SetPessimisticLeaseLockAcquisitionTimeout(TimeSpan value)
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Lease lock acquisition timeout must be greater than zero.");
            }

            LeaseLockAcquisitionTimeout = value;
        }

        /// <summary>
        /// Set maximum saga persistence lease lock acquisition refresh delay.
        /// </summary>
        /// <param name="value"></param>
        public void SetPessimisticLeaseLockAcquisitionMaximumRefreshDelay(TimeSpan value)
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Lease lock acquisition maximum refresh delay must be between zero and 1 second");
            }

            if (value > TimeSpan.FromSeconds(1))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Lease lock acquisition maximum refresh delay must be between zero and 1 second");
            }

            LeaseLockAcquisitionMaximumRefreshDelay = value;
        }

        internal TimeSpan LeaseLockAcquisitionTimeout { get; private set; } = TimeSpan.FromSeconds(60);
        internal TimeSpan LeaseLockTime { get; private set; } = TimeSpan.FromSeconds(60);
        internal TimeSpan LeaseLockAcquisitionMaximumRefreshDelay { get; private set; } = TimeSpan.FromMilliseconds(20);
        internal bool EnablePessimisticLocking { get; private set; }
    }
}