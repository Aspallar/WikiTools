namespace WikiaClientLibrary
{
    internal static class HttpErrors
    {
        public static bool Is404NotFoundError(string message)
        {
            return message.IndexOf("(404)") != -1;
        }
    }
}
