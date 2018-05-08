using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSharpRaft
{
    // Snapshot represents an in-memory representation of the current state of the system.
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

    // The request sent to a server to start from the snapshot.
    public class SnapshotRecoveryRequest
    {
        public string LeaderName { get; set; }
        public int LastIndex { get; set; }
        public int LastTerm { get; set; }
        public List<Peer> Peers { get; set; }
        public byte[] State { get; set; }

        public SnapshotRecoveryRequest() {

        }

        // Creates a new Snapshot request.
        public SnapshotRecoveryRequest(string leaderName, Snapshot snapshot)
        {
            this.LeaderName = leaderName;
            this.LastIndex = snapshot.LastIndex;
            this.LastTerm = snapshot.LastTerm;
            this.Peers = snapshot.Peers;
            this.State = snapshot.State;
        }

        // Encodes the SnapshotRecoveryRequest to a buffer. Returns the number of bytes
        // written and any error that may have occurred.
        public void Encode(Stream stream)
        {
            List<protobuf.SnapshotRecoveryRequest.Peer> protoPeers = new List<protobuf.SnapshotRecoveryRequest.Peer>();
            foreach (var peer in this.Peers)
            {
                protoPeers.Add(new protobuf.SnapshotRecoveryRequest.Peer()
                {
                    Name = peer.Name,
                    ConnectionString = peer.ConnectionString
                });
            }

            protobuf.SnapshotRecoveryRequest pb = new protobuf.SnapshotRecoveryRequest()
            {
                LeaderName = this.LeaderName,
                LastIndex = (uint)this.LastIndex,
                LastTerm = (uint)this.LastTerm,
                Peers = protoPeers,
                State = this.State,
            };
            Serializer.Serialize<protobuf.SnapshotRecoveryRequest>(stream, pb);
        }

        // Decodes the SnapshotRecoveryRequest from a buffer. Returns the number of bytes read and
        // any error that occurs.
        public void Decode(Stream stream)
        {
            protobuf.SnapshotRecoveryRequest pb = Serializer.Deserialize<protobuf.SnapshotRecoveryRequest>(stream);

            if (pb != null)
            {
                this.LeaderName = pb.LeaderName;
                this.LastIndex = (int)pb.LastIndex;
                this.LastTerm = (int)pb.LastTerm;
                this.State = pb.State;
                
                this.Peers = new List<Peer>();
                foreach (var peer in pb.Peers)
                {
                    this.Peers.Add(new Peer(peer.Name, peer.ConnectionString));
                }
            }
        }
    }

    // The response returned from a server appending entries to the log.
    public class SnapshotRecoveryResponse
    {
        public int Term { get; set; }

        public bool Success { get; set; }
        public int CommitIndex { get; set; }

        // Creates a new Snapshot response.
        public SnapshotRecoveryResponse(int term, bool success, int commitIndex)
        {

            this.Term = term;
            this.Success = success;
            this.CommitIndex = commitIndex;
        }

        // Encode writes the response to a writer.
        // Returns the number of bytes written and any error that occurs.
        public void Encode(Stream stream)
        {
            protobuf.SnapshotRecoveryResponse pb = new protobuf.SnapshotRecoveryResponse()
            {
                Term = (uint)this.Term,
                Success = this.Success,
                CommitIndex = (uint)this.CommitIndex,
            };
            Serializer.Serialize<protobuf.SnapshotRecoveryResponse>(stream, pb);
        }

        // Decodes the SnapshotRecoveryResponse from a buffer.
        public void Decode(Stream stream)
        {
            protobuf.SnapshotRecoveryResponse pb = Serializer.Deserialize<protobuf.SnapshotRecoveryResponse>(stream);
            if (pb != null)
            {
                this.Term = (int)pb.Term;
                this.Success = pb.Success;
                this.CommitIndex = (int)pb.CommitIndex;
            }
        }
    }

    // The request sent to a server to start from the snapshot.
    public class SnapshotRequest
    {
        public string LeaderName { get; set; }
        public int LastIndex { get; set; }
        public int LastTerm { get; set; }

        public SnapshotRequest()
        {

        }

        // Creates a new Snapshot request.
        public SnapshotRequest(string leaderName, Snapshot snapshot)
        {
            LeaderName = leaderName;
            LastIndex = snapshot.LastIndex;
            LastTerm = snapshot.LastTerm;
        }

        // Encodes the SnapshotRequest to a buffer. Returns the number of bytes
        // written and any error that may have occurred.
        public void Encode(Stream stream)
        {
            protobuf.SnapshotRequest pb = new protobuf.SnapshotRequest()
            {
                LeaderName = this.LeaderName,
                LastIndex = (uint)this.LastIndex,
                LastTerm = (uint)this.LastTerm,
            };

            Serializer.Serialize<protobuf.SnapshotRequest>(stream, pb);

        }

        // Decodes the SnapshotRequest from a buffer. Returns the number of bytes read and
        // any error that occurs.
        public void Decode(Stream stream)
        {
            protobuf.SnapshotRequest pb = Serializer.Deserialize<protobuf.SnapshotRequest>(stream);
            if (pb != null)
            {
                this.LeaderName = pb.LeaderName;
                this.LastIndex = (int)pb.LastIndex;
                this.LastTerm = (int)pb.LastTerm;
            }
        }
    }

    // The response returned if the follower entered snapshot state
    public class SnapshotResponse
    {
        public bool Success { get; set; }

        // Creates a new Snapshot response.
        public SnapshotResponse(bool success)
        {
            this.Success = success;
        }

        // Encodes the SnapshotResponse to a buffer. Returns the number of bytes
        // written and any error that may have occurred.
        public void Encode(Stream stream)
        {
            protobuf.SnapshotResponse pb = new protobuf.SnapshotResponse()
            {
                Success = this.Success

            };
            Serializer.Serialize<protobuf.SnapshotResponse>(stream, pb);
        }

        // Decodes the SnapshotResponse from a buffer. Returns the number of bytes read and
        // any error that occurs.
        public void Decode(Stream stream)
        {
            protobuf.SnapshotResponse pb = Serializer.Deserialize<protobuf.SnapshotResponse>(stream);

            this.Success = pb.Success;
        }
    }
}