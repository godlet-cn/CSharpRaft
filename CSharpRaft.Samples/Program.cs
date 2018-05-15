using CSharpRaft.Command;
using System;
using System.IO;
using System.Linq;

namespace CSharpRaft.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            Commands.RegisterCommand(new WriteCommand());

            bool trace = false;
            bool debug = false;
            string host = "";
            int port = 4006;
            string join = "";
            string path = "";

            var result = CommandLine.Parser.Default.ParseArguments<Flags>(args);
            if (!result.Errors.Any())
            {
                Flags options = result.Value;
                trace = options.Trace;
                debug = options.Debug;
                host = options.Host;
                port = options.Port;
                join = options.Join;
                path = options.Path;
            }
            else
            {
                Flags.PrintUsage();
                return;
            }

            if (trace)
            {
                DebugTrace.Level = LogLevel.TRACE;
                Console.WriteLine("Raft trace debugging enabled.");
            }
            else if (debug)
            {
                DebugTrace.Level = LogLevel.DEBUG;
                Console.WriteLine("Raft debugging enabled.");
            }

            if (!string.IsNullOrEmpty(path))
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }

            if (string.IsNullOrEmpty(host) || port == 0)
            {
                host = "localhost";
                port = 4006;
            }

            Server server = new Server(path, host, port);
            server.ListenAndServer(join);

            // Print system exit method
            while (true)
            {
                string str = Console.ReadLine();
                if (str == "exit" || str == "quit")
                {
                    server.Stop();
                    break;
                }
                Console.WriteLine("please type 'exit' or 'quit' to exit this application...");
            }
        }
    }
}
