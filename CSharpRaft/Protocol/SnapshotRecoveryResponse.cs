using ProtoBuf;
using System.IO;

namespace CSharpRaft.Protocol
{

    /// <summary>
    /// The response returned from a server appending entries to the log.
    /// </summary>
    public class SnapshotRecoveryResponse
    {
        public int Term { get; set; }

        public bool Success { get; set; }
        public int CommitIndex { get; set; }

        public SnapshotRecoveryResponse()
        {

        }

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
}
