using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UploadFiles
{
    internal static class FileSource
    {
        public static IEnumerable<string> GetFiles(string pattern, string filename, char filenameSeparator)
        {
            if (!string.IsNullOrEmpty(filename))
            {
                return GetListFileFilenames(filename, filenameSeparator);
            }
            else
            {
                return GetPatternFileNames(pattern);
            }
        }

        private static IEnumerable<string> GetPatternFileNames(string pattern)
        {
            try
            {
                string folder = Path.GetDirectoryName(pattern);
                if (string.IsNullOrEmpty(folder))
                    folder = ".";
                string patternPart = Path.GetFileName(pattern);
                return Directory.EnumerateFiles(folder, patternPart);
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
