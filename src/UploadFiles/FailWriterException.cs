using System;
using System.Runtime.Serialization;

namespace UploadFiles
{
    [Serializable]
    internal class FailWriterException : Exception
    {
        public FailWriterException()
        {
        }

        public FailWriterException(string message) : base(message)
        {
        }

        public FailWriterException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FailWriterException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}