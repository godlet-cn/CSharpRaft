using CSharpRaft.Command;
using CSharpRaft.Samples.Handlers;
using CSharpRaft.Router;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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

            var transporter = new CSharpRaft.Transport.HttpTransporter();

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
                this.Join(leader);
            }
            else if (this.raftServer.IsLogEmpty)
            {
                // Initialize the server by joining itself.
                CSharpRaft.DebugTrace.DebugLine("Initializing new cluster");
                this.raftServer.Do(new DefaultJoinCommand()
                {
                    Name = this.raftServer.Name,
                    ConnectionString = this.connectionString(),
                });
            }

            Console.WriteLine("Initializing HTTP server");
            Console.WriteLine("Listening at:" + connectionString());

            this.httpServer = new HttpServer();
            this.httpServer.AddHandler("/db/key", new ReadWriteHttpHandler(this.raftServer));
            this.httpServer.AddHandler("/join", new JoinHttpHandler(this.raftServer));

            transporter.Install(this.raftServer,(pattern,handler)=>
            {
                this.httpServer.AddHandler(pattern, handler);
            });

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

        private async void Join(string leader)
        {
            DefaultJoinCommand cmd = new DefaultJoinCommand()
            {
                Name=this.raftServer.Name,
                ConnectionString=this.connectionString()
            };

            using (MemoryStream ms = new MemoryStream())
            {
                cmd.Encode(ms);
                ms.Flush();

                HttpClient client = new HttpClient();
                HttpContent content = new StreamContent(ms);

                HttpResponseMessage resp = await client.PostAsync(string.Format("{0}/join", leader), content);
                if (resp != null)
                {

                }
            }
        }
    }
}
