using System;
using System.Runtime.Serialization;

namespace TourneyPairings
{
    [Serializable]
    internal class InvalidInputFileFormat : Exception
    {
        public InvalidInputFileFormat()
        {
        }

        public InvalidInputFileFormat(string message) : base(message)
        {
        }

        public InvalidInputFileFormat(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidInputFileFormat(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override string Message => "Invalid line:" + base.Message;
    }
}