using System;
using System.Runtime.Serialization;

namespace UploadFiles
{
    [Serializable]
    internal class OptionsException : Exception
    {
        public OptionsException()
        {
        }

        public OptionsException(string message) : base(message)
        {
        }

        public OptionsException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected OptionsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}