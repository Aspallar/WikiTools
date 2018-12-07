using System;
using System.Runtime.Serialization;

namespace TourneyPairings
{
    [Serializable]
    internal class InvalidConfig : Exception
    {
        public InvalidConfig()
        {
        }

        public InvalidConfig(string message) : base(message)
        {
        }

        public InvalidConfig(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidConfig(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override string Message
        {
            get
            {
                return "Invalid json in configuration file";
            }
        }
    }
}