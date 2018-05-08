using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace CSharpRaft.Test.Mocks
{
    class TestServer
    {
        static TestServer()
        {
            Commands.RegisterCommand(new Command1 { });
            Commands.RegisterCommand(new Command2 { });
        }

        public const int TestHeartbeatInterval = 50;
        public const int TestElectionTimeout = 200;

        public static Server NewTestServer(string name, Transporter transporter,StateMachine statemachine=null)
        {
            string path = Path.Combine(Path.GetTempPath(), "raft-server-");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            RaftEventHandler fn = (sender, e) =>
            {
                Server ser = sender as Server;
                DebugTrace.Warn("[{0}] {0} -> {1}\n", ser.Name, e.PrevValue, e.Value);
            };

            Server server = new Server(name, path, transporter, statemachine, null, "");
            server.StateChanged += fn;
            server.LeaderChanged += fn;
            server.TermChanged += fn;
            return server;
        }

        public static Server NewTestServerWithPath(string name, Transporter transporter, string path)
        {
            return new Server(name, path, transporter, null, null, "");
        }

        public static Server NewTestServerWithLog(string name, Transporter transporter, List<LogEntry> entries)
        {
            Server server = NewTestServer(name, transporter);
            string logPath = server.LogPath;
            using (FileStream file = File.Open(logPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                foreach (var entry in entries)
                {
                    entry.Encode(file);
                }
            }
            return server;
        }

        public static List<Server> NewTestCluster(List<string> names, Transporter transporter, Dictionary<string, Server> lookup)
        {
            List<Server> servers = new List<Server>();

            LogEntry e0 = new LogEntry(new Log(), null, 1, 1, new Command1() { Val = "v123" });

            foreach (var name in names)
            {
                if (lookup[name] != null)
                {
                    throw new Exception("raft: Duplicate server in test cluster! " + name);
                }
                Server server = NewTestServerWithLog("1", transporter, new List<LogEntry>() { e0 });
                server.ElectionTimeout=TestElectionTimeout;
                servers.Add(server);
                lookup[name] = server;
            }
            foreach (var server in servers)
            {
                server.HeartbeatInterval=TestHeartbeatInterval;
                server.Start();
                foreach (var peer in servers)
                {
                    server.AddPeer(peer.Name, "");
                }
            }
            return servers;
        }
    }
}
