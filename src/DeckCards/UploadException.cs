using System;
using System.Runtime.Serialization;

namespace DeckCards
{
    [Serializable]
    internal class UploadException : Exception
    {
        public UploadException()
        {
        }

        public UploadException(string message) : base(message)
        {
        }

        public UploadException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UploadException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}