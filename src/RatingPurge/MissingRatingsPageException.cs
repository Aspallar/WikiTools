using System;
using System.Runtime.Serialization;

namespace RatingPurge
{
    [Serializable]
    internal class MissingRatingsPageException : Exception
    {
        public MissingRatingsPageException()
        {
        }

        public MissingRatingsPageException(string message) : base(message)
        {
        }

        public MissingRatingsPageException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MissingRatingsPageException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}