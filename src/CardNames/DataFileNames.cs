using System.IO;
using System.Reflection;

namespace CardNames
{
    internal class DataFileNames
    {
        private string _applicationPath;

        public string DictionaryMain => _applicationPath + "en_us.dic";
        public string DictionaryAff => _applicationPath + "en_us.aff";
        public string CardData => _applicationPath + "Cards.json";

        public DataFileNames()
        {
            string location = Assembly.GetEntryAssembly().Location;
            string path = Path.GetDirectoryName(location);
            _applicationPath = path + "\\";
        }
    }
}
