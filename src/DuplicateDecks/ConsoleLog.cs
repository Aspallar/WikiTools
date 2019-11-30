using System;

namespace DuplicateDecks
{
    internal class ConsoleLog : ILog
    {
        private readonly bool _useError;

        public ConsoleLog(bool useError)
        {
            _useError = useError;
        }

        public void Log(string message)
        {
            if (_useError)
                Console.Error.WriteLine(message);
            else
                Console.WriteLine(message);
        }
    }
}
