using System;
using System.IO;

namespace CSharpRaft.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            Commands.RegisterCommand(new WriteCommand());

            string serverName = "test";
            string path = Path.Combine(Path.GetTempPath(), serverName);

            Server server = new Server(serverName, path, new HttpTransporter(), new DefaultStateMachine(),new KeyValueDB(),"");
            
            server.Start();

            server.Do(new JoinCommand { Name = server.Name });
            server.Do(new WriteCommand("foo", "bar"));

            server.AddPeer("peer1","");
            
            Console.ReadKey();
        }
    }
}
