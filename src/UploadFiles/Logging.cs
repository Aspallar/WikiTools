using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using System.IO;
using System;

namespace UploadFiles
{
    internal static class Logging
    {
        public static void Configure(string logConfigFileName, string logFilename, bool colored)
        {
            if (File.Exists(logConfigFileName))
            {
                if (!string.IsNullOrEmpty(logFilename))
                    log4net.GlobalContext.Properties["LogFileName"] = logFilename;
                using (Stream sr = File.OpenRead(logConfigFileName))
                    XmlConfigurator.Configure(sr);
            }
            else
            {
                ConfigureDefaultLogger(logFilename, colored);
            }
        }

        private static void ConfigureDefaultLogger(string logFileName, bool colored)
        {

            var layout = new PatternLayout("%d %-5level %message%newline");
            layout.ActivateOptions();

            IAppender consoleAppender = GetDefaultConsoleAppender(colored, layout);
            
            if (string.IsNullOrEmpty(logFileName))
            {
                BasicConfigurator.Configure(consoleAppender);
            }
            else
            {
                IAppender fileAppender = GetDefaultFileAppender(logFileName, layout);
                BasicConfigurator.Configure(consoleAppender, fileAppender);
            }
        }

        private static FileAppender GetDefaultFileAppender(string logFileName, PatternLayout layout)
        {
            var fileAppender = new FileAppender
            {
                Layout = layout,
                AppendToFile = true,
                File = logFileName,
                Threshold = Level.Info,
            };
            fileAppender.ActivateOptions();
            return fileAppender;
        }

        private static IAppender GetDefaultConsoleAppender(bool colored, PatternLayout layout)
        {
            IAppender consoleAppender;
            if (colored)
            {
                ColoredConsoleAppender appender = CreateColoredConsoleAppender(layout);
                appender.ActivateOptions();
                consoleAppender = appender;
            }
            else
            {
                var appender = new ConsoleAppender
                {
                    Layout = layout,
                    Threshold = Level.Info,
                };
                appender.ActivateOptions();
                consoleAppender = appender;
            }

            return consoleAppender;
        }


        private static ColoredConsoleAppender CreateColoredConsoleAppender(PatternLayout layout)
        {
            var appender = new ColoredConsoleAppender
            {
                Layout = layout,
                Threshold = Level.Info,

            };
            appender.AddMapping(new ColoredConsoleAppender.LevelColors
            {
                Level = Level.Info,
                ForeColor = ColoredConsoleAppender.Colors.Green
            });
            appender.AddMapping(new ColoredConsoleAppender.LevelColors
            {
                Level = Level.Warn,
                ForeColor = ColoredConsoleAppender.Colors.Yellow
            });
            appender.AddMapping(new ColoredConsoleAppender.LevelColors
            {
                Level = Level.Error,
                ForeColor = ColoredConsoleAppender.Colors.Red | ColoredConsoleAppender.Colors.HighIntensity
            });
            appender.AddMapping(new ColoredConsoleAppender.LevelColors
            {
                Level = Level.Fatal,
                ForeColor = ColoredConsoleAppender.Colors.White | ColoredConsoleAppender.Colors.HighIntensity,
                BackColor = ColoredConsoleAppender.Colors.Red
            });
            return appender;
        }
    }
}
