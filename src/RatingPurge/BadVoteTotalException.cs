using System;
using System.Runtime.Serialization;

namespace RatingPurge
{
    [Serializable]
    internal class BadVoteTotalException : Exception
    {
        public BadVoteTotalException()
        {
        }

        public BadVoteTotalException(string message) : base(message)
        {
        }

        public BadVoteTotalException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BadVoteTotalException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}