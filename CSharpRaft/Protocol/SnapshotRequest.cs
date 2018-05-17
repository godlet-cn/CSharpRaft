using ProtoBuf;
using System.IO;

namespace CSharpRaft.Protocol
{

    /// <summary>
    /// The request sent to a server to start from the snapshot.
    /// </summary>
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
}
