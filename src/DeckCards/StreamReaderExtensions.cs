using System.IO;

namespace DeckCards
{
    internal static class StreamReaderExtensions
    {
        public static string ReadConfigLine(this StreamReader sr)
        {
            while (true)
            {
                string line = sr.ReadLine();
                if (line != null)
                {
                    line = line.TrimEnd();
                    if (line.Length != 0 && line[0] != '#')
                        return line;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
