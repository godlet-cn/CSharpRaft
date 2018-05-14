using CSharpRaft.Transport;
using System;
using System.Threading.Tasks;

namespace CSharpRaft.Test
{
    class TestTransporter : ITransporter
    {
        public Func<Server, Peer, RequestVoteRequest, Task<RequestVoteResponse>> SendVoteRequestFunc;
        public Func<Server, Peer, AppendEntriesRequest, Task<AppendEntriesResponse>> SendAppendEntriesRequestFunc;
        public Func<Server, Peer, SnapshotRequest, Task<SnapshotResponse>> SendSnapshotRequestFunc;
        public Func<Server, Peer, SnapshotRecoveryRequest, Task<SnapshotRecoveryResponse>> SendSnapshotRecoveryRequestFunc;

        Task<AppendEntriesResponse> ITransporter.SendAppendEntriesRequest(Server server, Peer peer, AppendEntriesRequest req)
        {
            return SendAppendEntriesRequestFunc(server, peer, req);
        }

        Task<RequestVoteResponse> ITransporter.SendVoteRequest(Server server, Peer peer, RequestVoteRequest req)
        {
            return SendVoteRequestFunc(server, peer, req);
        }

        Task<SnapshotResponse> ITransporter.SendSnapshotRequest(Server server, Peer peer, SnapshotRequest req)
        {
            return SendSnapshotRequestFunc(server, peer, req);
        }

        Task<SnapshotRecoveryResponse> ITransporter.SendSnapshotRecoveryRequest(Server server, Peer peer, SnapshotRecoveryRequest req)
        {
            return SendSnapshotRecoveryRequestFunc(server, peer, req);
        }
    }
}
