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

        ServerState State();

        string GetPath();

        string LogPath();

        string SnapshotPath(int lastIndex, int lastTerm);

        int Term();

        int CommitIndex();

        string VotedFor();

        int MemberCount();

        int QuorumSize();

        bool IsLogEmpty();

        List<LogEntry> LogEntries();

        string LastCommandName();

        string GetState();

        int ElectionTimeout();

        void SetElectionTimeout(int duration);

        int HeartbeatInterval();

        void SetHeartbeatInterval(int duration);

        Transporter Transporter();

        void SetTransporter(Transporter t);

        AppendEntriesResponse AppendEntries(AppendEntriesRequest req);

        RequestVoteResponse RequestVote(RequestVoteRequest req);

        SnapshotResponse RequestSnapshot(SnapshotRequest req);

        SnapshotRecoveryResponse SnapshotRecoveryRequest(SnapshotRecoveryRequest req);

        void AddPeer(string name, string connectiongString);

        void RemovePeer(string name);

        Dictionary<string, Peer> Peers();

        void Init();

        void Start();

        void Stop();

        bool Running();

        object Do(Command command);

        void TakeSnapshot();

        void LoadSnapshot();

        void FlushCommitIndex();
    }
}
