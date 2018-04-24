using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UploadFiles
{
    internal class FileSource
    {
        public IEnumerable<string> Files { get; private set; }

        public FileSource(string pattern, string filename, char filenameSeparator)
        {
            if (!string.IsNullOrEmpty(filename))
            {
                Files = GetListFileFilenames(filename, filenameSeparator);
            }
            else
            {
                Files = GetPatternFileNames(pattern);
            }
        }

        private IEnumerable<string> GetPatternFileNames(string pattern)
        {
            try
            {
                string folder = Path.GetDirectoryName(pattern);
                if (string.IsNullOrEmpty(folder))
                    folder = ".";
                return Directory.EnumerateFiles(folder, pattern);
            }
            catch (DirectoryNotFoundException)
            {
                return Enumerable.Empty<string>();
            }
        }

        private static IEnumerable<string> GetListFileFilenames(string fileName, char filenameSeparator)
        {
            using (var reader = new StreamReader(fileName))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    int pos = line.IndexOf(filenameSeparator);
                    if (pos != -1)
                        line = line.Substring(0, pos);
                    string trimmedLine = line.Trim();
                    if (trimmedLine.Length > 0)
                        yield return trimmedLine;
                }
            }
        }
    }
}
