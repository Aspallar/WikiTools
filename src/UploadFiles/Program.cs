using CommandLine;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WikiToolsShared;

namespace UploadFiles
{
    class Program
    {
        private static ILog log = LogManager.GetLogger(typeof(Program));
        private const char filenameSeparator = '|';
        static int cancel = 0;

        static void Main(string[] args)
        {
            try
            {
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(options => Run(options));
            }
            catch (UploadFilesFatalException ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
            catch (HttpRequestException ex)
            {
                if (ex.InnerException == null)
                    Console.Error.WriteLine(ex.Message);
                else
                    Console.Error.WriteLine(ex.InnerException.Message);
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
#if !DEBUG
            catch(Exception ex)
            {
                ShowExceptionMessages(ex);
            }
#endif
        }

        private static void Run(Options options)
        {
            options.Validate();
            Logging.Configure("UploadFiles.logging.xml", options.Log, !options.NoColor);
            log.Info($"Uploading to {options.Site}");
            if (options.WaitFiles <= 0 || options.WaitTime <= 0)
                log.Info("No waiting between uploads");
            else
                log.Info($"Waiting for {options.WaitTime} seconds every {options.WaitFiles} uploads.");
            RunAsync(options).GetAwaiter().GetResult();
        }

        private static async Task RunAsync(Options options)
        {
            var waiter = new Waiter(options.WaitFiles, options.WaitTime);
            using (FileUploader uploader = new FileUploader(options.Site, GetPageText(options), options.Category, options.Comment))
            using (FailWriter failWriter = new FailWriter(options.Fails, filenameSeparator))
            {
                string username = GetUsername(options);
                string password = GetPassword(options);
                if (!await uploader.LoginAsync(username, password))
                {
                    Console.Error.WriteLine("Unable to log in.");
                    return;
                }
                IEnumerable<string> files = GetFilesToUpload(options);
                Console.CancelKeyPress += Console_CancelKeyPress;
                foreach (string file in files)
                {
                    if (cancel != 0)
                        break; // foreach file

                    if (!HasValidFileType(file))
                    {
                        string msg = $" Unsupported file type \"{Path.GetExtension(file)}\".";
                        log.Error($"[{file}]{msg}");
                        failWriter.Write(file, msg);
                        continue; // foreach file
                    }
                    if (!File.Exists(file))
                    {
                        string msg = " Not found.";
                        log.Error($"[{file}]{msg}");
                        failWriter.Write(file, msg);
                        continue; // foreach file
                    }
                    log.Info($"Uploading [{file}] ");
                    UploadResponse response = await uploader.UpLoadAsync(file, options.Force);
                    if (response.Result == ResponseCodes.Success)
                    {
                        log.Info($"[{file}] Uploaded");
                    }
                    else if (response.Result == ResponseCodes.Warning)
                    {
                        string warningsText = GetWarningsText(response);
                        log.Warn($"[{file}]{warningsText}");
                        failWriter.Write(file, warningsText);
                    }
                    else
                    {
                        log.Fatal($"Unexpected response from wiki site.\n{response.Xml}");
                        break; // foreach file
                    }
                    await waiter.Wait();
                }
            }
        }

        private static string GetPageText(Options options)
        {
            if (options.NoContent)
                return string.Empty;
            if (string.IsNullOrEmpty(options.Content))
                return Properties.Settings.Default.DefaultText.Replace("\\n", "\n");
            return File.ReadAllText(options.Content);
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
            if (!string.IsNullOrEmpty(options.List))
            {
                return GetListFileFilenames(options.List);
            }

            try
            {
                string pattern = Path.GetFileName(options.FilePattern);
                string folder = Path.GetDirectoryName(options.FilePattern);
                if (string.IsNullOrEmpty(folder))
                    folder = ".";
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

        private static bool HasValidFileType(string filename)
        {
            string[] validTypes = { ".png", ".gif", ".jpg",
                ".jpeg", ".ico", ".pdf", ".svg", ".odt", ".ods",
                ".odp", ".odg", ".odc", ".odf", ".odi", ".odm",
                ".ogg", ".ogv", ".oga" };

            string extension = Path.GetExtension(filename).ToLowerInvariant();
            return validTypes.Contains(extension);
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

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (e.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                Interlocked.Increment(ref cancel);
                e.Cancel = true;
                log.Warn("Ctrl-C Pressed. Uploads will stop after current upload finishes.");
            }
        }

        private static void ShowExceptionMessages(Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            if (ex.InnerException != null)
                ShowExceptionMessages(ex.InnerException);
        }

    }
}
