using CSharpRaft.Command;
using CSharpRaft.Transport;
using System.Collections.Generic;

namespace CSharpRaft
{
    public interface IServer
    {
        string Name { get; }

        object Context { get; }

        IStateMachine StateMachine { get; }

        string Leader { get; }

        ServerState State { get; }

        string Path { get; }

        string LogPath { get; }
        
        int Term { get; }

        int CommitIndex { get; }

        string VotedFor { get; }

        int MemberCount { get; }

        int QuorumSize { get; }

        bool IsLogEmpty { get; }

        List<LogEntry> LogEntries { get; }

        string LastCommandName { get; }

        int ElectionTimeout { get; set; }

        int HeartbeatInterval { get; set; }

        ITransporter Transporter { get; set; }

        bool IsRunning { get; }

        void Init();

        void Start();

        void Stop();

        object Do(ICommand command);

        AppendEntriesResponse AppendEntries(AppendEntriesRequest req);

        RequestVoteResponse RequestVote(RequestVoteRequest req);

        SnapshotResponse RequestSnapshot(SnapshotRequest req);

        SnapshotRecoveryResponse SnapshotRecoveryRequest(SnapshotRecoveryRequest req);

        void AddPeer(string name, string connectiongString);

        void RemovePeer(string name);

        Dictionary<string, Peer> GetPeers();

        Snapshot GetSnapshot();

        string GetSnapshotPath(int lastIndex, int lastTerm);

        void TakeSnapshot();

        void LoadSnapshot();

        void FlushCommitIndex();

        event RaftEventHandler StateChanged;

        event RaftEventHandler LeaderChanged;

        event RaftEventHandler TermChanged;

        event RaftEventHandler Commited;

        event RaftEventHandler PeerAdded;

        event RaftEventHandler PeerRemoved;

        event RaftEventHandler HeartbeatIntervalReached;

        event RaftEventHandler ElectionTimeoutThresholdReached;

        event RaftEventHandler HeartbeatReached;
    }
}
