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
    // TODO: validate file types (PNG, JPG etc). Skip invalid ones.
    // TODO: implement prompt

    class Program
    {
        private const char filenameSeparator = '|';

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
                    Console.Error.WriteLine("Unable to log in.");
                    return;
                }
                IEnumerable<string> files = GetFilesToUpload(options);
                foreach (string file in files)
                {
                    Console.Write($"Uploading [{file}] ");
                    UploadResponse response = await uploader.UpLoadAsync(file, options.Force);
                    if (response.Result == ResponseCodes.Success)
                    {
                        Console.WriteLine("SUCCESS");
                    }
                    else if (response.Result == ResponseCodes.Warning)
                    {
                        string warningsText = GetWarningsText(response);
                        if (!string.IsNullOrEmpty(options.Fails))
                            File.AppendAllText(options.Fails, file + filenameSeparator + warningsText + "\n");
                        Console.Write("WARNING");
                        Console.WriteLine(warningsText);
                    }
                    else
                    {
                        Console.WriteLine("UNEXPECTED RESPONSE");
                        Console.WriteLine(response.Xml);
                        break; // foreach file
                    }
                }
            }
        }

        private static string GetWarningsText(UploadResponse response)
        {
            StringBuilder text = new StringBuilder();
            if (response.AlreadyExists)
                text.Append(" Already exists.");
            if (response.BadFilename)
                text.Append(" Invalid file name.");
            if (response.IsDuplicate)
            {
                text.Append(" Duplicate of");
                foreach (string duplicate in response.Duplicates)
                    text.Append($" [{duplicate}]");
                text.Append(".");
            }
            return text.ToString();
        }

        private static IEnumerable<string> GetFilesToUpload(Options options)
        {
            // TODO: handle file does not exist
            if (!string.IsNullOrEmpty(options.List))
            {
                return GetListFileFilenames(options.List);
            }

            try
            {
                string fullPattern = options.FilePattern;
                string pattern = Path.GetFileName(fullPattern);
                string folder = Path.GetDirectoryName(fullPattern);
                return Directory.EnumerateFiles(folder, pattern);
            }
            catch (DirectoryNotFoundException)
            {
                return Enumerable.Empty<string>();
            }
        }

        private static IEnumerable<string> GetListFileFilenames(string fileName)
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
