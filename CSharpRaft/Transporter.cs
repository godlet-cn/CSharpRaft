namespace CSharpRaft
{
    public interface Transporter
    {
        RequestVoteResponse SendVoteRequest(Server server, Peer peer, RequestVoteRequest req);

        AppendEntriesResponse SendAppendEntriesRequest(Server server, Peer peer, AppendEntriesRequest req);

        SnapshotResponse SendSnapshotRequest(Server server, Peer peer, SnapshotRequest req);

        SnapshotRecoveryResponse SendSnapshotRecoveryRequest(Server server, Peer peer, SnapshotRecoveryRequest req);

    }
}