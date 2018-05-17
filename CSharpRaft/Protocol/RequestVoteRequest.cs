using ProtoBuf;
using System.IO;

namespace CSharpRaft.Protocol
{
    /// <summary>
    /// The request sent to a server to vote for a candidate to become a leader.
    /// </summary>
    public class RequestVoteRequest
    {
        internal Peer peer;

        public int Term { get; set; }

        public int LastLogIndex { get; set; }

        public int LastLogTerm { get; set; }

        public string CandidateName { get; set; }

        public RequestVoteRequest()
        {

        }

        public RequestVoteRequest(int term, string candidateName, int lastLogIndex, int lastLogTerm)
        {
            this.Term = term;
            this.CandidateName = candidateName;
            this.LastLogIndex = lastLogIndex;
            this.LastLogTerm = lastLogTerm;
        }

        /// <summary>
        /// Encodes the RequestVoteRequest to a stream.
        /// </summary>
        /// <param name="stream"></param>
        public void Encode(Stream stream)
        {
            Serializer.Serialize<protobuf.RequestVoteRequest>(stream, new protobuf.RequestVoteRequest()
            {
                Term = (uint)this.Term,
                LastLogTerm = (uint)LastLogTerm,
                CandidateName = CandidateName,
                LastLogIndex = (uint)LastLogIndex
            });
        }

        /// <summary>
        ///  Decodes the RequestVoteRequest from a stream. 
        /// </summary>
        /// <param name="stream"></param>
        public void Decode(Stream stream)
        {
            protobuf.RequestVoteRequest pb = Serializer.Deserialize<protobuf.RequestVoteRequest>(stream);

            this.Term = (int)pb.Term;
            this.LastLogTerm = (int)pb.LastLogTerm;
            this.LastLogIndex = (int)pb.LastLogIndex;
            this.CandidateName = pb.CandidateName;
        }
    }
}