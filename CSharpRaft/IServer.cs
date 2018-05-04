using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpRaft
{
    public interface IServer
    {
        string Name();

        object Context();

        StateMachine StateMachine();

        string Leader();

        string State();

        string Path();

        string LogPath();

        string SnapshotPath(UInt64 lastIndex, UInt64 lastTerm);

        UInt64 Term();

        UInt64 CommitIndex();

        string VotedFor();

        int MemberCount();

        int QuorumSize();

        bool IsLogEmpty();

        LogEntry[] LogEntries();

        string LastCommandName();

        string GetState();

        TimeSpan ElectionTimeout();

        void SetElectionTimeout(TimeSpan duration);

        TimeSpan HeartbeatInterval();

        void SetHeartbeatInterval(TimeSpan duration);

        Transporter Transporter();

        void SetTransporter(Transporter t);

        AppendEntriesResponse AppendEntries(AppendEntriesRequest req);

        RequestVoteResponse RequestVote(RequestVoteRequest req);

        SnapshotResponse RequestSnapshot(SnapshotRequest req);

        SnapshotRecoveryResponse SnapshotRecoveryRequest(SnapshotRecoveryRequest req);

        bool AddPeer(string name, string connectiongString);

        bool RemovePeer(string name);

        Dictionary<string, Peer> Peers();

        bool Init();

        bool Start();

        void Stop();

        bool Running();

        object Do(Command command);

        bool TakeSnapshot();

        bool LoadSnapshot();

        void AddEventListener(string id, EventListener listenner);

        void FlushCommitIndex();
    }
}
