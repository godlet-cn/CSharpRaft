using System;

namespace CSharpRaft.Test
{
    class TestTransporter : Transporter
    {
        public Func<Server, Peer, RequestVoteRequest, RequestVoteResponse> SendVoteRequestFunc;
        public Func<Server, Peer, AppendEntriesRequest, AppendEntriesResponse> SendAppendEntriesRequestFunc;
        public Func<Server, Peer, SnapshotRequest, SnapshotResponse> SendSnapshotRequestFunc;
        public Func<Server, Peer, SnapshotRecoveryRequest, SnapshotRecoveryResponse> SendSnapshotRecoveryRequestFunc;

        public RequestVoteResponse SendVoteRequest(Server server, Peer peer, RequestVoteRequest req)
        {
            return SendVoteRequestFunc(server, peer, req);
        }

        public AppendEntriesResponse SendAppendEntriesRequest(Server server, Peer peer, AppendEntriesRequest req)
        {
            return SendAppendEntriesRequestFunc(server, peer, req);
        }

        public SnapshotResponse SendSnapshotRequest(Server server, Peer peer, SnapshotRequest req)
        {
            return SendSnapshotRequestFunc(server, peer, req);
        }

        public SnapshotRecoveryResponse SendSnapshotRecoveryRequest(Server server, Peer peer, SnapshotRecoveryRequest req)
        {
            return SendSnapshotRecoveryRequestFunc(server, peer, req);
        }
    }
}
