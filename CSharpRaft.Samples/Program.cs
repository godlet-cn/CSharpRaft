using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CSharpRaft.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            string serverName = "test";
            string path = Path.Combine(Path.GetTempPath(), serverName);
            Server server = new Server(serverName, path, new HttpTransporter(), new DefaultStateMachine(),null,"");

            server.Start();

        }
    }
}
