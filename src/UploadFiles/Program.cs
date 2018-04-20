using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WikiToolsShared;

namespace UploadFiles
{
    class Program
    {
        static void Main(string[] args)
        {
#if !DEBUG
            const string issuesUrl = "https://github.com/Aspallar/WikiTools/issues";
            try
            {
#endif
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(options => RunAsync(options).GetAwaiter().GetResult());
                Utils.Pause("Done. Press a key");
#if !DEBUG
            }
            catch(Exception ex)
            {
                Utils.WriteError($"An unexpected error occurred. You should report this at {issuesUrl}");
                Console.Error.WriteLine($"Holy gobbledygook Batman! Include this in the issue.\n{ex.ToString()}\n");
                Utils.Pause("Press any key to raise issue.");
                Process.Start(issuesUrl);
            }
#endif
        }

        private static async Task RunAsync(Options options)
        {
            string username = GetUsername(options);
            string password = GetPassword(options);
            using (FileUploader uploader = new FileUploader(options.Site))
            {
                if (!await uploader.LoginAsync(username, password))
                {
                    Console.WriteLine("Unable to log in. Invalid credentials or site not available.");
                    return;
                }
                IEnumerable<string> files = GetFilesToUpload(options);
                foreach (string file in files)
                {
                    Console.Write($"Uploading [{file}] ");
                    UploadResponse response = await uploader.UpLoadAsync(file);
                    if (response.Result == ResponseCodes.Success)
                    {
                        Console.WriteLine("SUCCESS");
                    }
                    else
                    {
                        Console.Write("WARNING");
                        if (response.AlreadyExists)
                            Console.Write(" Already exists.");
                        if (response.Duplicates.Count > 0)
                        {
                            Console.Write(" Duplicate of");
                            foreach (string duplicate in response.Duplicates)
                                Console.Write($" [{duplicate}]");
                            Console.Write(".");
                        }
                        Console.WriteLine();
                    }

                }
            }
        }
        
        private static IEnumerable<string> GetFilesToUpload(Options options)
        {
            // TODO: files from a --list option
            try
            {
                string fullPattern = options.FileNames;
                string pattern = Path.GetFileName(fullPattern);
                string folder = Path.GetDirectoryName(fullPattern);
                return Directory.EnumerateFiles(folder, pattern);
            }
            catch (DirectoryNotFoundException)
            {
                return Enumerable.Empty<string>();
            }
        }

        private static string GetPassword(Options options)
        {
            if (!string.IsNullOrEmpty(options.Password))
                return options.Password;
            Console.Write("Password: ");
            string password = Utils.ReadPasswordFromConsole();
            Console.WriteLine();
            return password;
        }

        private static string GetUsername(Options options)
        {
            if (!string.IsNullOrEmpty(options.User))
                return options.User;
            Console.Write("Username: ");
            string userName = Console.ReadLine();
            return userName;
        }
    }
}
