using CommandLine;
using System;
using System.Reflection;

namespace CSharpRaft.Samples
{
    class Flags
    {
        [Option("trace", DefaultValue = false, HelpText = "Raft trace debugging")]
        public bool Trace { get; set; }

        [Option("debug", DefaultValue = false, HelpText = "Raft debugging")]
        public bool Debug { get; set; }

        [Option('h', "hostname", DefaultValue = "localhost", HelpText = "hostname")]
        public string Host { get; set; }

        [Option('p', "port", HelpText = "port")]
        public int Port { get; set; }

        [Option("join", DefaultValue = "", HelpText = "host:port of leader to join")]
        public string Join { get; set; }

        [Option('o', "path", Required = true, HelpText = "Data path")]
        public string Path { get; set; }

        public static void PrintUsage()
        {
            Console.WriteLine("Usage: {0} [arguments] -o <data-path> \n",
                    System.IO.Path.GetFileName(Assembly.GetExecutingAssembly().Location));
        }
    }
}
