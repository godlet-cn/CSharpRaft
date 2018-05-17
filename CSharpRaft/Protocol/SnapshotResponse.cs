using ProtoBuf;
using System.IO;

namespace CSharpRaft.Protocol
{

    /// <summary>
    /// The response returned if the follower entered snapshot state
    /// </summary>
    public class SnapshotResponse
    {
        public bool Success { get; set; }

        public SnapshotResponse()
        {

        }
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
