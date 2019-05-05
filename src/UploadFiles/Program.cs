using CommandLine;
using log4net;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WikiToolsShared;

namespace UploadFiles
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        private const char filenameSeparator = '|';
        private static CancellationTokenSource cancelSource;

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
            if (System.Diagnostics.Debugger.IsAttached)
                Utils.Pause("Press a key.");
        }

        private static void Run(Options options)
        {
            options.Validate();
            if (options.Site.ToLowerInvariant().StartsWith("https"))
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.Expect100Continue = true;
            }
            OpeningMessage();
            Logging.Configure("UploadFiles.logging.xml", options.Log, !options.NoColor, options.Debug);
            LogOptions(options);

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
                log.Debug(ex.ToString());
            }
#endif
        }

        private static void OpeningMessage()
        {
            Console.Error.WriteLine("Press Ctrl-C to stop uploads once current upload completes.");
            Console.Error.WriteLine("Press Ctrl-Break to stop uploads immediately.");
        }

        private static void LogOptions(Options options)
        {
            log.Info($"Uploading to {options.Site}");
            if (!string.IsNullOrEmpty(options.FilePattern))
                log.Info($"Uploading files that match \"{options.FilePattern}\"");
            if (!string.IsNullOrEmpty(options.List))
                log.Info($"Uploading files specified in file \"{options.List}\"");
            if (options.WaitFiles <= 0 || options.WaitTime <= 0)
                log.Info("No waiting between uploads");
            else
                log.Info($"Waiting for {options.WaitTime} seconds every {options.WaitFiles} uploads.");
            if (options.Force)
                log.Info("Forcing upload on warnings (--force specified).");
            else
                log.Info("Not forcing upload on warnings (no --force specified).");
            if (!options.Site.ToUpperInvariant().StartsWith("HTTPS"))
                log.Warn("Wiki access, including login, is not secure. Use https: in site for secure access.");
        }

        private static async Task RunAsync(Options options)
        {
            var waiter = new Waiter(options.WaitFiles, options.WaitTime, options.Delay, cancelSource.Token, log);
            using (var uploader = new FileUploader(options.Site, GetPageText(options), options.Category, options.Comment, options.Timeout))
            using (var failWriter = new FailWriter(options.Fails, filenameSeparator))
            {
                string username = GetUsername(options);
                string password = GetPassword(options);
                if (!await uploader.LoginAsync(username, password, options.NoFileTypeCheck))
                {
                    Console.Error.WriteLine("Unable to log in.");
                    log.Fatal("Login failed.");
                    return;
                }
                Console.CancelKeyPress += Console_CancelKeyPress;
                foreach (string file in FileSource.GetFiles(options.FilePattern, options.List, filenameSeparator))
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
                if (!uploader.IsPermittedFile(file))
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
                    string warningsText = response.WarningsText;
                    log.Warn($"[{file}]{warningsText}");
                    failWriter.Write(file, warningsText);
                }
                else if (response.Result != "Success")
                {
                    if (response.IsError)
                    {
                        foreach (ApiError error in response.Errors)
                        {
                            log.Error($"[{file}] {error.Code} -> {error.Info}");
                            failWriter.Write(file, $" ERROR {error.Info}");
                        }
                    }
                    else
                    {
                        log.Fatal($"Unexpected response from wiki site while uploading file [{file}].\n{response.Xml}");
                        return true;
                    }
                }
            }
            catch (IOException ex)
            {
                LogExceptionMessages(ex);
                failWriter.Write(file, "IO error.");
            }
            catch (TaskCanceledException)
            {
                log.Error($"[{file}] Upload timed out.");
                failWriter.Write(file, "Upload timed out.");
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
                log.Warn("Ctrl-C Pressed. Uploads will stop after current upload, if any, finishes.");
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
