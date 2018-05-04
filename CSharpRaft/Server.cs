using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpRaft
{
    public class Server : IServer
    {
        public void AddEventListener(string id, EventListener listenner)
        {
            throw new NotImplementedException();
        }

        public bool AddPeer(string name, string connectiongString)
        {
            throw new NotImplementedException();
        }

        public AppendEntriesResponse AppendEntries(AppendEntriesRequest req)
        {
            throw new NotImplementedException();
        }

        public ulong CommitIndex()
        {
            throw new NotImplementedException();
        }

        public object Context()
        {
            throw new NotImplementedException();
        }

        public object Do(Command command)
        {
            throw new NotImplementedException();
        }

        public TimeSpan ElectionTimeout()
        {
            throw new NotImplementedException();
        }

        public void FlushCommitIndex()
        {
            throw new NotImplementedException();
        }

        public string GetState()
        {
            throw new NotImplementedException();
        }

        public TimeSpan HeartbeatInterval()
        {
            throw new NotImplementedException();
        }

        public bool Init()
        {
            throw new NotImplementedException();
        }

        public bool IsLogEmpty()
        {
            throw new NotImplementedException();
        }

        public string LastCommandName()
        {
            throw new NotImplementedException();
        }

        public string Leader()
        {
            throw new NotImplementedException();
        }

        public bool LoadSnapshot()
        {
            throw new NotImplementedException();
        }

        public LogEntry[] LogEntries()
        {
            throw new NotImplementedException();
        }

        public string LogPath()
        {
            throw new NotImplementedException();
        }

        public int MemberCount()
        {
            throw new NotImplementedException();
        }

        public string Name()
        {
            throw new NotImplementedException();
        }

        public string Path()
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, Peer> Peers()
        {
            throw new NotImplementedException();
        }

        public int QuorumSize()
        {
            throw new NotImplementedException();
        }

        public bool RemovePeer(string name)
        {
            throw new NotImplementedException();
        }

        public SnapshotResponse RequestSnapshot(SnapshotRequest req)
        {
            throw new NotImplementedException();
        }

        public RequestVoteResponse RequestVote(RequestVoteRequest req)
        {
            throw new NotImplementedException();
        }

        public bool Running()
        {
            throw new NotImplementedException();
        }

        public void SetElectionTimeout(TimeSpan duration)
        {
            throw new NotImplementedException();
        }

        public void SetHeartbeatInterval(TimeSpan duration)
        {
            throw new NotImplementedException();
        }

        public void SetTransporter(Transporter t)
        {
            throw new NotImplementedException();
        }

        public string SnapshotPath(ulong lastIndex, ulong lastTerm)
        {
            throw new NotImplementedException();
        }

        public SnapshotRecoveryResponse SnapshotRecoveryRequest(SnapshotRecoveryRequest req)
        {
            throw new NotImplementedException();
        }

        public bool Start()
        {
            throw new NotImplementedException();
        }

        public string State()
        {
            throw new NotImplementedException();
        }

        public StateMachine StateMachine()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public bool TakeSnapshot()
        {
            throw new NotImplementedException();
        }

        public ulong Term()
        {
            throw new NotImplementedException();
        }

        public Transporter Transporter()
        {
            throw new NotImplementedException();
        }

        public string VotedFor()
        {
            throw new NotImplementedException();
        }
    }
}
