using CSharpRaft.Protocol;
using System.Threading.Tasks;

namespace CSharpRaft.Transport
{
    public interface ITransporter
    {
        Task<AppendEntriesResponse> SendAppendEntriesRequest(Server server, Peer peer, AppendEntriesRequest req);

        Task<RequestVoteResponse> SendVoteRequest(Server server, Peer peer, RequestVoteRequest req);

        Task<SnapshotResponse> SendSnapshotRequest(Server server, Peer peer, SnapshotRequest req);

        Task<SnapshotRecoveryResponse> SendSnapshotRecoveryRequest(Server server, Peer peer, SnapshotRecoveryRequest req);
    }
}