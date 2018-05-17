using ProtoBuf;
using System.Collections.Generic;
using System.IO;

namespace CSharpRaft.Protocol
{
    /// <summary>
    /// The request sent to a server to start from the snapshot.
    /// </summary>
    public class SnapshotRecoveryRequest
    {
        public string LeaderName { get; set; }
        public int LastIndex { get; set; }
        public int LastTerm { get; set; }
        public List<Peer> Peers { get; set; }
        public byte[] State { get; set; }

        public SnapshotRecoveryRequest()
        {

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

}
