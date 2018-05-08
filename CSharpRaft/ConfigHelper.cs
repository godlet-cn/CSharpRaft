using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSharpRaft
{
    public static class ConfigHelper
    {
        /// <summary>
        /// write server's configuration to file.
        /// </summary>
        /// <param name="server"></param>
        public static void WriteConfig(Server server)
        {
            List<Peer> peers = new List<Peer>();
            foreach (var peerItem in server.GetPeers())
            {
                peers.Add(peerItem.Value.Clone());
            }

            Config conf = new Config()
            {
                CommitIndex = server.log.CommitIndex,
                Peers = peers
            };
            string strConf = JsonConvert.SerializeObject(conf);

            string confPath = System.IO.Path.Combine(server.Path, "conf");
            server.debugLine("writeConf.write ", confPath);

            string tmpConfPath = System.IO.Path.Combine(server.Path, "conf.tmp");
            using (StreamWriter stream = new StreamWriter(tmpConfPath, false, Encoding.UTF8))
            {
                stream.Write(strConf);
            }

            if (File.Exists(confPath))
            {
                File.Replace(tmpConfPath, confPath, confPath + ".bak");
            }
            else
            {
                File.Move(tmpConfPath, confPath);
            }
        }

        /// <summary>
        /// Read the configuration for the server.
        /// </summary>
        /// <param name="server"></param>
        public static void ReadConfig(Server server)
        {
            string confPath = System.IO.Path.Combine(server.Path, "conf");

            server.debugLine("readConf.open ", confPath);

            if (File.Exists(confPath))
            {
                using (StreamReader stream = new StreamReader(confPath, Encoding.UTF8))
                {
                    string strConf = stream.ReadToEnd();
                    Config conf = JsonConvert.DeserializeObject<Config>(strConf);
                    if (conf != null)
                    {
                        server.log.updateCommitIndex(conf.CommitIndex);
                    }
                }
            }
        }
    }
}
