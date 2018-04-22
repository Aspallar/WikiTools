using System;
using System.IO;

namespace UploadFiles
{
    internal class FailWriter : IDisposable
    {
        private char _separator;
        private TextWriter _writer;

        public FailWriter(string fileName, char separator)
        {
            _separator = separator;
            if (!string.IsNullOrEmpty(fileName))
                _writer = new StreamWriter(fileName);
        }

        public void Write(string fileName, string message)
        {
            if (_writer != null)
            {
                _writer.WriteLine(fileName + "|" + message);
                _writer.Flush();
            }
        }

        public void Dispose()
        {
            if (_writer != null)
            {
                _writer.Close();
                _writer = null;
            }
        }
    }
}
