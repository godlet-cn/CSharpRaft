using ProtoBuf;
using System.IO;

namespace CSharpRaft.Protocol
{
    /// <summary>
    /// The response returned from a server appending entries to the log.
    /// </summary>
    public class AppendEntriesResponse
    {
        internal protobuf.AppendEntriesResponse pb;

        internal string peer;

        internal bool append;

        public AppendEntriesResponse()
        {

        }

        // Creates a new AppendEntries response.
        public AppendEntriesResponse(int term, bool success, int index, int commitIndex)
        {
            this.pb = new protobuf.AppendEntriesResponse()
            {
                Term = (uint)term,
                Index = (uint)index,
                Success = success,
                CommitIndex = (uint)commitIndex,
            };
        }

        public int Index
        {
            get
            {
                return (int)this.pb.Index;
            }
        }

        public int CommitIndex
        {
            get
            {
                return (int)this.pb.CommitIndex;
            }
        }

        public int Term
        {
            get
            {
                return (int)this.pb.Term;
            }
        }

        public bool Success
        {
            get
            {
                return this.pb.Success;
            }
        }

        // Encodes the AppendEntriesResponse to a buffer. Returns the number of bytes
        // written and any error that may have occurred.
        public void Encode(Stream stream)
        {
            Serializer.Serialize<protobuf.AppendEntriesResponse>(stream, pb);
        }

        // Decodes the AppendEntriesResponse from a buffer. Returns the number of bytes read and
        // any error that occurs.
        public void Decode(Stream stream)
        {
            this.pb = Serializer.Deserialize<protobuf.AppendEntriesResponse>(stream);
        }
    }
}
