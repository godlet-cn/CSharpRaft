using CSharpRaft.Samples.Handlers;
using GodletRouter;
using System;
using System.IO;
using System.Text;

namespace CSharpRaft.Samples
{
    class Server
    {
        private string name;
        private string host;
        private int port;
        private string path;

        private CSharpRaft.Server raftServer;
        private HttpServer httpServer;
        private KeyValueDB db;

        public Server(string path, string host, int port)
        {
            this.path = path;
            this.host = host;
            this.port = port;
            this.db = new KeyValueDB();

            string serverNameFile = Path.Combine(path, "name");
            if (File.Exists(serverNameFile))
            {
                using (StreamReader sr = new StreamReader(serverNameFile, Encoding.UTF8))
                {
                    this.name = sr.ReadToEnd();
                }
            }
            else
            {
                using (StreamWriter sr = new StreamWriter(serverNameFile, false, Encoding.UTF8))
                {
                    this.name = Guid.NewGuid().ToString("N");
                    sr.Write(this.name);
                }
            }
        }

        // Returns the connection string.
        public string connectionString()
        {
            return string.Format("http://{0}:{1}/", this.host, this.port);
        }

        /// <summary>
        /// Start serve client request.
        /// </summary>
        /// <param name="leader"></param>
        public void ListenAndServer(string leader)
        {
            CSharpRaft.DebugTrace.DebugLine("Initializing Raft Server: " + this.path);

            var transporter = new CSharpRaft.HttpTransporter();

            this.raftServer = new CSharpRaft.Server(this.name, this.path, transporter, null, db, "");

            this.raftServer.Start();

            if (string.IsNullOrEmpty(leader) == false)
            {
                // Join to leader if specified.
                CSharpRaft.DebugTrace.DebugLine("Attempting to join leader:" + leader);
                if (!this.raftServer.IsLogEmpty)
                {
                    throw new Exception("Cannot join with an existing log");
                }
                if (this.Join(leader) == false)
                {
                    throw new Exception("Join leader Failed");
                }
            }
            else if (this.raftServer.IsLogEmpty)
            {
                // Initialize the server by joining itself.
                CSharpRaft.DebugTrace.DebugLine("Initializing new cluster");
                this.raftServer.Do(new CSharpRaft.DefaultJoinCommand()
                {
                    Name = this.raftServer.Name,
                    ConnectionString = this.connectionString(),
                });
            }

            Console.WriteLine("Initializing HTTP server");
            Console.WriteLine("Listening at:" + connectionString());

            this.httpServer = new HttpServer();
            this.httpServer.AddHandler("/db/key", new ReadWriteHttpHandler());
            this.httpServer.AddHandler("/join", new JoinHttpHandler());
            this.httpServer.Start(this.host, this.port);
        }
        
        /// <summary>
        /// Stop service
        /// </summary>
        public void Stop()
        {
            try
            {
                this.httpServer.Stop();
                this.raftServer.Stop();
            }
            catch (Exception err)
            {
                Console.WriteLine("Fatal:error stop server" + err.Message);
            }
        }

        private bool Join(string leader)
        {
            return true;
        }
    }
}
