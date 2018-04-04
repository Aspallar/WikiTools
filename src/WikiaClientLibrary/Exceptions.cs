using System;

namespace WikiaClientLibrary
{

    [Serializable]
    public class WikiaEditConflictException : Exception
    {
        public WikiaEditConflictException() : base("Edit conflict.") { }
        protected WikiaEditConflictException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class WikiaEditException : Exception
    {
        public string ErrorCode { get; private set; }

        public WikiaEditException(EditError error) : base(error.Info)
        {
            ErrorCode = error.Code;
        }

        protected WikiaEditException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class WikiaUnknownResponseException : Exception
    {
        public WikiaUnknownResponseException() { }
        public WikiaUnknownResponseException(string message) : base(message) { }
        public WikiaUnknownResponseException(string message, Exception inner) : base(message, inner) { }
        protected WikiaUnknownResponseException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

}
