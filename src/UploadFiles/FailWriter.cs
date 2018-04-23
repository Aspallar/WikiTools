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
            try
            {
                _separator = separator;
                if (!string.IsNullOrEmpty(fileName))
                    _writer = new StreamWriter(fileName);
            }
            catch (IOException ex)
            {
                throw new FailWriterException("Unable to create file file", ex);
            }
        }
            
        public void Write(string fileName, string message)
        {
            try
            {
                if (_writer != null)
                {
                    _writer.WriteLine(fileName + "|" + message);
                    _writer.Flush();
                }
            }
            catch (IOException ex)
            {
                throw new FailWriterException("Error while writing to fail file", ex);
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
