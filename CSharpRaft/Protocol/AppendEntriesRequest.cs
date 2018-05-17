using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;

namespace CSharpRaft.Protocol
{
    /// <summary>
    /// The request sent to a server to append entries to the log.
    /// </summary>
    public class AppendEntriesRequest
    {
        public int Term { get; set; }

        public int PrevLogIndex { get; set; }

        public int PrevLogTerm { get; set; }

        public int CommitIndex { get; set; }

        public string LeaderName { get; set; }

        public List<protobuf.LogEntry> Entries { get; set; }

        public AppendEntriesRequest()
        {

        }

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

        /// <summary>
        ///  Encodes the AppendEntriesRequest to a stream.
        /// </summary>
        /// <param name="stream"></param>
        public void Encode(Stream stream)
        {
            try
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
            catch (Exception err)
            {
                DebugTrace.TraceLine("transporter.ae.encoding.error:" + err);
            }
        }

        /// <summary>
        /// Decodes the AppendEntriesRequest from a buffer.
        /// </summary>
        /// <param name="stream"></param>
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

}