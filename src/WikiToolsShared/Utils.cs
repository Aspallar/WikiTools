using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WikiToolsShared
{
    public static class Utils
    {
        public static void WriteError(string errorMessage)
        {
            string batman = $"Holy {RobinSays()} Batman!";
            Console.Error.WriteLine(batman);
            Console.Error.WriteLine(errorMessage);
        }

        public static string RobinSays()
        {
            string[] robin = {
                "bouncing bunnies", "pulsating penguins", "kinky kangaroos",
                "rampaging ferocidon", "kippers", "deviant dinos", "pernicious pirates",
                "jank deck", "weird wiki", "Vraska", "Liliana", "trampling toad",
                "smoking Samut"
            };
            Random rand = new Random();
            return robin[rand.Next(robin.Length)];
        }

        public static void Pause(string message)
        {
            Console.Error.WriteLine(message);
            while (Console.KeyAvailable)
                Console.ReadKey(true);
            Console.ReadKey(true);
        }


        public static string ReadPasswordFromConsole()
        {
            var password = new StringBuilder();
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (!char.IsControl(key.KeyChar))
                {
                    password.Append(key.KeyChar);
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        password.Remove(password.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
            } while (key.Key != ConsoleKey.Enter);
            return password.ToString();
        }

        public static string UserAgent()
        {
            AssemblyName name = Assembly.GetEntryAssembly().GetName();
            return $"{name.Name}/{VersionString(name)} (contact https://magicarena.fandom.com/wiki/Special:ListUsers/sysop)";
        }

        public static string VersionString()
        {
            return VersionString(Assembly.GetEntryAssembly().GetName());
        }

        public static string VersionString(AssemblyName assemblyName)
        {
            var version = assemblyName.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        public static int CombineHashCodes(int hashCode1, int hashCode2)
            => (((hashCode1 << 5) + hashCode1) ^ hashCode2);

        public static void InitialiseTls()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.Expect100Continue = true;
        }
    }
}
