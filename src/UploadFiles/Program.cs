﻿using CommandLine;
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
        static CancellationTokenSource cancelSource;

        static void Main(string[] args)
        {
            try
            {
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(options => Run(options));
            }
            catch (OptionsException ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
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

#if !DEBUG
            try
            {
#endif
                using (cancelSource = new CancellationTokenSource())
                    RunAsync(options).GetAwaiter().GetResult();
#if !DEBUG
            }
            catch (Exception ex)
            {
                LogFatalExceptionMessages(ex);
            }
#endif
        }

        private static async Task RunAsync(Options options)
        {
            var waiter = new Waiter(options.WaitFiles, options.WaitTime, cancelSource.Token, log);
            using (var uploader = new FileUploader(options.Site, GetPageText(options), options.Category, options.Comment))
            using (var failWriter = new FailWriter(options.Fails, filenameSeparator))
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
                    if (cancelSource.IsCancellationRequested)
                        break;
                    if (await UploadFile(file, waiter, failWriter, uploader, options.Force))
                        break;
                }
            }
        }

        private static async Task<bool> UploadFile(string file, Waiter waiter, FailWriter failWriter, FileUploader uploader, bool force)
        {
            try
            {
                if (!HasValidFileType(file))
                {
                    string msg = $" Unsupported file type \"{Path.GetExtension(file)}\".";
                    log.Error($"[{file}]{msg}");
                    failWriter.Write(file, msg);
                    return false;
                }
                if (!File.Exists(file))
                {
                    string msg = " Not found.";
                    log.Error($"[{file}]{msg}");
                    failWriter.Write(file, msg);
                    return false;
                }
                log.Info($"Uploading [{file}]");
                UploadResponse response = await uploader.UpLoadAsync(file, force);
                if (response.Result == ResponseCodes.Warning)
                {
                    string warningsText = GetWarningsText(response);
                    log.Warn($"[{file}]{warningsText}");
                    failWriter.Write(file, warningsText);
                }
                else if (response.Result != "Success")
                {
                    log.Fatal($"Unexpected response from wiki site while uploading file {file}.\n{response.Xml}");
                    return true;
                }
            }
            catch (IOException ex)
            {
                LogExceptionMessages(ex);
                failWriter.Write(file, "IO error encountered.");
            }
            await waiter.Wait();
            return false;
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
                log.Warn("Ctrl-C Pressed. Uploads will stop after current upload finishes.");
                e.Cancel = true;
                cancelSource.Cancel();
            }
        }

        private static void LogFatalExceptionMessages(Exception ex)
        {
            log.Fatal(ex.Message);
            if (ex.InnerException != null)
                LogFatalExceptionMessages(ex.InnerException);
        }

        private static void LogExceptionMessages(Exception ex)
        {
            log.Error(ex.Message);
            if (ex.InnerException != null)
                LogExceptionMessages(ex.InnerException);
        }

    }
}
