using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UploadFilesTestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options => Run(options));
        }

        private static void Run(Options options)
        {
            var rand = new Random();
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:10202/");
            Console.WriteLine("Listening...");
            listener.Start();
            long count = 0;
            while (true)
            {
                string reply = "";
                var context = listener.GetContext();
                ++count;
                var request = context.Request;
                var response = context.Response;

                Console.WriteLine($"Received Request No: {count}");
                Console.WriteLine($"RawUrl: {request.RawUrl}");
                if (request.HasEntityBody)
                {
                    if (request.ContentType == "application/x-www-form-urlencoded")
                    {
                        Console.WriteLine("Type: Login");
                        reply = "<?xml version=\"1.0\"?><api><login result=\"Success\" /></api>";
                    }
                    else if (request.ContentType.StartsWith("multipart/form-data"))
                    {
                        Console.WriteLine("Type: Image upload");
                        if (options.Exists > 0 && rand.Next(100) < options.Exists)
                            reply = "<?xml version=\"1.0\"?><api><upload result=\"Warning\"><warnings exists=\"foo\"></warnings></upload></api>";
                        else
                            reply = "<?xml version=\"1.0\"?><api><upload result=\"Success\"></upload></api>";
                        if (options.Delay > 0)
                            Thread.Sleep(options.Delay);
                    }
                    request.InputStream.Close();
                }
                else
                {
                    if (request.RawUrl.IndexOf("query&list=users&usprop=groups") != -1)
                        reply = "<?xml version=\"1.0\"?><api><query><users><user><groups><g>autoconfirmed</g></groups></user></users></query></api>";
                    else if (request.RawUrl.IndexOf("action=query&prop=info&intoken=edit&titles=Foo") != -1)
                        reply = "<?xml version=\"1.0\"?><api><query><pages><page edittoken=\"+foobar\"></page></pages></query></api>";
                }

                response.StatusCode = 200;
                byte[] buffer = Encoding.UTF8.GetBytes(reply);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
                Console.WriteLine($"Response Sent:\n{reply}");
            }
        }
    }

}
