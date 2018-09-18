using System;
using System.Runtime.Serialization;

namespace RatingPurge
{
    [Serializable]
    internal class OptionValidationException : Exception
    {
        public OptionValidationException()
        {
        }

        public OptionValidationException(string message) : base(message)
        {
        }

        public OptionValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected OptionValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}