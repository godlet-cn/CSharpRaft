using ProtoBuf;
using System.IO;

namespace CSharpRaft.Protocol
{
    /// <summary>
    /// The response returned from a server after a vote for a candidate to become a leader.
    /// </summary>
    public class RequestVoteResponse
    {

        internal Peer peer;

        public int Term { get; set; }

        public bool VoteGranted { get; set; }

        public RequestVoteResponse()
        {

        }

        // Creates a new RequestVote response.
        public RequestVoteResponse(int term, bool voteGranted)
        {
            this.Term = term;
            this.VoteGranted = voteGranted;
        }


        /// <summary>
        /// Encodes the RequestVoteResponse to a stream.
        /// </summary>
        /// <param name="stream"></param>
        public void Encode(Stream stream)
        {
            Serializer.Serialize<protobuf.RequestVoteResponse>(stream, new protobuf.RequestVoteResponse()
            {
                Term = (uint)this.Term,
                VoteGranted = VoteGranted
            });
        }

        /// <summary>
        /// Decodes the RequestVoteResponse from a stream. 
        /// </summary>
        /// <param name="stream"></param>
        public void Decode(Stream stream)
        {
            protobuf.RequestVoteResponse pb = Serializer.Deserialize<protobuf.RequestVoteResponse>(stream);

            this.Term = (int)pb.Term;
            this.VoteGranted = pb.VoteGranted;
        }
    }

}
