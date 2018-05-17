using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSharpRaft.Protocol
{
    /// <summary>
    /// Snapshot represents an in-memory representation of the current state of the system.
    /// </summary>
    public class Snapshot
    {
        public int LastIndex { get; set; }

        public int LastTerm { get; set; }

        // Cluster configuration.
        public List<Peer> Peers { get; set; }

        public byte[] State { get; set; }

        public string Path { get; set; }

        // save writes the snapshot to file.
        internal void save()
        {
            using (FileStream file = new FileStream(this.Path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    // Serialize to JSON.
                    string strSnapshot = JsonConvert.SerializeObject(this);

                    byte[] cmdData = UTF8Encoding.UTF8.GetBytes(strSnapshot);
                    ms.Write(cmdData, 0, cmdData.Length);

                    ms.Seek(0, SeekOrigin.Begin);

                    // Generate checksum and write it to disk.
                    Crc32 crc32 = new Crc32();
                    string hash = crc32.CheckSum(ms);
                    byte[] hastData = Encoding.Default.GetBytes(hash + Environment.NewLine);
                    file.Write(hastData, 0, hastData.Length);

                    // Write the snapshot to disk.
                    byte[] data = ms.ToArray();
                    file.Write(data, 0, data.Length);

                    file.Flush();
                }
            }
        }

        // remove deletes the snapshot file.
        internal void remove()
        {
            File.Delete(this.Path);
        }
    }
}