using Newtonsoft.Json;
using System;
using System.IO;

namespace TourneyPairings
{
    internal class Config
    {
        [JsonProperty("separatorline")]
        public string SeparatorLine { get; private set; }

        [JsonProperty("lineformat")]
        public string LineFormat { get; private set; }

        public Config()
        {
            SeparatorLine = "|-";
            LineFormat = "| %n || %1 || - || %2";
        }

        public static Config LoadFromFile(string configFileName, string inputFileName)
        {
            Config config;
            if (string.IsNullOrEmpty(configFileName))
            {
                string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TourneyPairings.config.json");
                if (File.Exists(defaultPath))
                    config = LoadFromFile(defaultPath);
                else
                    config = new Config();
            }
            else
            {
                string path = FindConfigFile(configFileName, inputFileName);
                config = LoadFromFile(path);
            }
            return config;
        }

        private static Config LoadFromFile(string path)
        {
            try
            {
                using (var sr = new StreamReader(path))
                    return JsonConvert.DeserializeObject<Config>(sr.ReadToEnd());
            }
            catch (JsonReaderException)
            {
                throw new InvalidConfig();
            }
        }

        private void ReadConfig(string configFileName, string inputFileName)
        {
            string fileName = FindConfigFile(configFileName, inputFileName);
        }

        private static string FindConfigFile(string configFileName, string inputFileName)
        {
            if (Path.IsPathRooted(configFileName))
            {
                if (!File.Exists(configFileName))
                    throw new FileNotFoundException("Config file not found: "+ configFileName);
                return configFileName;
            }

            string fileName = Path.Combine(Path.GetDirectoryName(inputFileName), configFileName);
            if (File.Exists(fileName))
                return fileName;

            if (File.Exists(configFileName))
                return configFileName;

            fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFileName);
            if (File.Exists(fileName))
                return fileName;

            throw new FileNotFoundException("Config file not found: " + configFileName);
        }
    }
}