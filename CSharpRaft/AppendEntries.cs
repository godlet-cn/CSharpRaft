using ProtoBuf;
using System.Collections.Generic;
using System.IO;

namespace CSharpRaft
{
    // The request sent to a server to append entries to the log.
    public class AppendEntriesRequest
    {
        public int Term { get; set; }

        public int PrevLogIndex { get; set; }

        public int PrevLogTerm { get; set; }

        public int CommitIndex { get; set; }

        public string LeaderName { get; set; }

        public List<protobuf.LogEntry> Entries { get; set; }

        // Creates a new AppendEntries request.
        public AppendEntriesRequest(int term, int prevLogIndex, int prevLogTerm,
            int commitIndex, string leaderName, List<LogEntry> entries)
        {
            List<protobuf.LogEntry> pbEntries = new List<protobuf.LogEntry>();

            foreach (var item in entries)
            {
                pbEntries.Add(item.pb);
            }

            this.Term = term;
            this.PrevLogIndex = prevLogIndex;
            this.PrevLogTerm = prevLogTerm;
            this.CommitIndex = commitIndex;
            this.LeaderName = leaderName;
            this.Entries = pbEntries;
        }
        
        // Encodes the AppendEntriesRequest to a buffer. Returns the number of bytes
        // written and any error that may have occurred.
        public void Encode(Stream stream)
        {
            protobuf.AppendEntriesRequest pb = new protobuf.AppendEntriesRequest()
            {
                Term = (uint)this.Term,
                PrevLogIndex = (uint)this.PrevLogIndex,
                PrevLogTerm = (uint)this.PrevLogTerm,
                CommitIndex = (uint)this.CommitIndex,
                LeaderName = this.LeaderName,
                Entries = this.Entries,
            };

            Serializer.Serialize<protobuf.AppendEntriesRequest>(stream, pb);
        }

        // Decodes the AppendEntriesRequest from a buffer. Returns the number of bytes read and
        // any error that occurs.
        public void Decode(Stream stream)
        {
            protobuf.AppendEntriesRequest pb = new protobuf.AppendEntriesRequest();
            pb = Serializer.Deserialize<protobuf.AppendEntriesRequest>(stream);

            this.Term = (int)pb.Term;
            this.PrevLogIndex = (int)pb.PrevLogIndex;
            this.PrevLogTerm = (int)pb.PrevLogTerm;

            this.CommitIndex = (int)pb.CommitIndex;

            this.LeaderName = pb.LeaderName;

            this.Entries = pb.Entries;
        }
    }

    // The response returned from a server appending entries to the log.
    public class AppendEntriesResponse
    {
        internal protobuf.AppendEntriesResponse pb;

        internal string peer;

        internal bool append;
        
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
            get {
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