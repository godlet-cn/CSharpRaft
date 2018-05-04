using System.IO;

namespace CSharpRaft
{

    // The request sent to a server to vote for a candidate to become a leader.
    public class RequestVoteRequest
    {
        public Peer peer;

        public ulong Term;

        public ulong LastLogIndex;

        public ulong LastLogTerm;

        public string CandidateName;

        public RequestVoteRequest(ulong term, string candidateName, ulong lastLogIndex, ulong lastLogTerm)
        {
            this.Term = term;
            this.CandidateName = candidateName;
            this.LastLogIndex = lastLogIndex;
            this.LastLogTerm = lastLogTerm;
        }

        // Encodes the RequestVoteRequest to a buffer. Returns the number of bytes
        // written and any error that may have occurred.
        public void Encode(BinaryWriter w)
        {
            string p = "";

            w.Write(p);
        }

        // Decodes the RequestVoteRequest from a buffer. Returns the number of bytes read and
        // any error that occurs.
        public RequestVoteRequest Decode(BinaryReader r)
        {
            return null;
        }
    }

    // The response returned from a server after a vote for a candidate to become a leader.
    public class RequestVoteResponse
    {

        public Peer peer;

        public ulong Term;

        public bool VoteGranted;


        // Creates a new RequestVote response.
        public RequestVoteResponse(ulong term, bool voteGranted)
        {
            this.Term = term;
            this.VoteGranted = voteGranted;
        }


        // Encodes the RequestVoteResponse to a buffer. Returns the number of bytes
        // written and any error that may have occurred.
        public void Encode(BinaryWriter w)
        {
            string p = "";

            w.Write(p);
        }

        // Decodes the RequestVoteResponse from a buffer. Returns the number of bytes read and
        // any error that occurs.
        public RequestVoteResponse Decode(BinaryReader r)
        {
            return null;
        }
    }

}