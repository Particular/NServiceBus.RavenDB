namespace NServiceBus.RavenDB.Tests
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    class LegacyAddress : ISerializable
    {
        public LegacyAddress(string queueName, string machineName, bool ignoreMachineName = false, string defaultMachineName = "")
        {
            Queue = queueName;
            queueLowerCased = queueName.ToLower();
            Machine = machineName ?? defaultMachineName;
            machineLowerCased = Machine.ToLower();
            this.ignoreMachineName = ignoreMachineName;
        }

        protected LegacyAddress(SerializationInfo info, StreamingContext context)
        {
            Queue = info.GetString("Queue");
            Machine = info.GetString("Machine");

            if (!string.IsNullOrEmpty(Queue))
            {
                queueLowerCased = Queue.ToLower();
            }

            if (!string.IsNullOrEmpty(Machine))
            {
                machineLowerCased = Machine.ToLower();
            }
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Queue", Queue);
            info.AddValue("Machine", Machine);
        }

        public LegacyAddress SubScope(string qualifier)
        {
            return new LegacyAddress(Queue + "." + qualifier, Machine);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ((queueLowerCased?.GetHashCode() ?? 0) * 397);

                if (!ignoreMachineName)
                {
                    hashCode ^= machineLowerCased?.GetHashCode() ?? 0;
                }
                return hashCode;
            }
        }

        public override string ToString()
        {
            if (ignoreMachineName)
                return Queue;

            return Queue + "@" + Machine;
        }

        public string Queue { get; }

        public string Machine { get; }

        /// <summary>
        /// Overloading for the == for the class LegacyAddress
        /// </summary>
        /// <param name="left">Left hand side of == operator</param>
        /// <param name="right">Right hand side of == operator</param>
        /// <returns>true if the LHS is equal to RHS</returns>
        public static bool operator ==(LegacyAddress left, LegacyAddress right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Overloading for the != for the class LegacyAddress
        /// </summary>
        /// <param name="left">Left hand side of != operator</param>
        /// <param name="right">Right hand side of != operator</param>
        /// <returns>true if the LHS is not equal to RHS</returns>
        public static bool operator !=(LegacyAddress left, LegacyAddress right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified <see cref="object"/> is equal to the current <see cref="object"/>; otherwise, false.
        /// </returns>
        /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="object"/>. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(LegacyAddress)) return false;
            return Equals((LegacyAddress)obj);
        }

        /// <summary>
        /// Check this is equal to other LegacyAddress
        /// </summary>
        /// <param name="other">reference addressed to be checked with this</param>
        /// <returns>true if this is equal to other</returns>
        private bool Equals(LegacyAddress other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (!ignoreMachineName && !other.machineLowerCased.Equals(machineLowerCased))
                return false;

            return other.queueLowerCased.Equals(queueLowerCased);
        }

        readonly string queueLowerCased;
        readonly string machineLowerCased;
        bool ignoreMachineName;
    }
}