using System;
using System.Runtime.Serialization;

namespace TourneyPairings
{
    [Serializable]
    internal class InvalidNameMap : Exception
    {
        public InvalidNameMap()
        {
        }

        public InvalidNameMap(string message) : base(message)
        {
        }

        public InvalidNameMap(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidNameMap(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override string Message
        {
            get
            {
                return "Invalid line in namemap.txt: " + base.Message;
            }
        }
    }
}