using System;
using System.Runtime.Serialization;

namespace DuplicateDecks
{
    [Serializable]
    internal class DuplicateDecksException : Exception
    {
        public DuplicateDecksException()
        {
        }

        public DuplicateDecksException(string message) : base(message)
        {
        }

        public DuplicateDecksException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DuplicateDecksException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}