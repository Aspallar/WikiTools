using System;
using System.Runtime.Serialization;

namespace TourneyPairings
{
    [Serializable]
    internal class InvalidEncoding : Exception
    {
        public InvalidEncoding()
        {
        }

        public InvalidEncoding(string message) : base(message)
        {
        }

        public InvalidEncoding(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidEncoding(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override string Message
        {
            get
            {
                return "Unsupported encoding " + base.Message;
            }
        }
    }
}