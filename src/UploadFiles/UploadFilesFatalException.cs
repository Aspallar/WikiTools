using System;
using System.Runtime.Serialization;

namespace UploadFiles
{
    [Serializable]
    internal class UploadFilesFatalException : Exception
    {
        public UploadFilesFatalException()
        {
        }

        public UploadFilesFatalException(string message) : base(message)
        {
        }

        public UploadFilesFatalException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UploadFilesFatalException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}