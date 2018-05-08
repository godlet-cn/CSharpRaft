using ProtoBuf;
using System.IO;

namespace CSharpRaft
{
    // The request sent to a server to vote for a candidate to become a leader.
    public class RequestVoteRequest
    {
        internal Peer peer;

        public int Term { get; set; }

        public int LastLogIndex { get; set; }

        public int LastLogTerm { get; set; }

        public string CandidateName { get; set; }

        public RequestVoteRequest(int term, string candidateName, int lastLogIndex, int lastLogTerm)
        {
            this.Term = term;
            this.CandidateName = candidateName;
            this.LastLogIndex = lastLogIndex;
            this.LastLogTerm = lastLogTerm;
        }

        // Encodes the RequestVoteRequest to a buffer. Returns the number of bytes
        // written and any error that may have occurred.
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

        // Decodes the RequestVoteRequest from a buffer. Returns the number of bytes read and
        // any error that occurs.
        public void Decode(Stream stream)
        {
            protobuf.RequestVoteRequest pb = Serializer.Deserialize<protobuf.RequestVoteRequest>(stream);

            this.Term = (int)pb.Term;
            this.LastLogTerm = (int)pb.LastLogTerm;
            this.LastLogIndex = (int)pb.LastLogIndex;
            this.CandidateName = pb.CandidateName;
        }
    }

    // The response returned from a server after a vote for a candidate to become a leader.
    public class RequestVoteResponse
    {

        internal Peer peer;

        public int Term { get; set; }

        public bool VoteGranted { get; set; }


        // Creates a new RequestVote response.
        public RequestVoteResponse(int term, bool voteGranted)
        {
            this.Term = term;
            this.VoteGranted = voteGranted;
        }


        // Encodes the RequestVoteResponse to a buffer. Returns the number of bytes
        // written and any error that may have occurred.
        public void Encode(Stream stream)
        {
            Serializer.Serialize<protobuf.RequestVoteResponse>(stream, new protobuf.RequestVoteResponse()
            {
                Term = (uint)this.Term,
                VoteGranted= VoteGranted
            });
        }

        // Decodes the RequestVoteResponse from a buffer. Returns the number of bytes read and
        // any error that occurs.
        public void Decode(Stream stream)
        {
            protobuf.RequestVoteResponse pb = Serializer.Deserialize<protobuf.RequestVoteResponse>(stream);

            this.Term = (int)pb.Term;
            this.VoteGranted = pb.VoteGranted;
        }
    }

}