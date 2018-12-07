using CommandLine;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace TourneyPairings
{
    internal class Options
    {
        [Value(0, Default = "round.txt", MetaName = "Input file name")]
        public string InputFileName { get;  set; }

        [Option(HelpText = "Don't output warnings")]
        public bool NoWarnings { get; set; }

        [Option(HelpText = "Name of config file to use")]
        public string Config { get; set; }

        [Option(HelpText = "Encoding of input file")]
        public string Enc { get; set; }

        public Encoding InputEncoding
        {
            get
            {
                Encoding encoding;

                if (string.IsNullOrEmpty(Enc))
                    encoding = Encoding.Default;
                else if (Regex.IsMatch(Enc, @"^\d+$"))
                {
                    try
                    {
                        encoding = Encoding.GetEncoding(int.Parse(Enc));
                    }
                    catch (NotSupportedException)
                    {
                        throw new InvalidEncoding(Enc);
                    }
                }
                else
                {
                    switch (Enc.ToUpperInvariant())
                    {
                        case "UTF8":
                            encoding = Encoding.UTF8; break;
                        case "UTF7":
                            encoding = Encoding.UTF7; break;
                        case "UTF32":
                            encoding = Encoding.UTF32; break;
                        case "UNI":
                            encoding = Encoding.Unicode; break;
                        case "BIGENDIAN":
                            encoding = Encoding.BigEndianUnicode; break;
                        case "ASCII":
                            encoding = Encoding.ASCII; break;
                        default:
                            throw new InvalidEncoding(Enc);
                    }
                }
                return encoding;
            }
        }
    }
}