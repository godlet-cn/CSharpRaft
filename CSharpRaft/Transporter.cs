 using System;

namespace CSharpRaft
{
    public interface Transporter
    {
        RequestVoteResponse SendVoteRequest(Server server, Peer peer, RequestVoteRequest req);

        AppendEntriesResponse SendAppendEntriesRequest(Server server, Peer peer, AppendEntriesRequest req);

        SnapshotResponse SendSnapshotRequest(Server server, Peer peer, SnapshotRequest req);

        SnapshotRecoveryResponse SendSnapshotRecoveryRequest(Server server, Peer peer, SnapshotRecoveryRequest req);
    }

    public class HttpTransporter : Transporter
    {
        public AppendEntriesResponse SendAppendEntriesRequest(Server server, Peer peer, AppendEntriesRequest req)
        {
            throw new NotImplementedException();
        }

        public SnapshotRecoveryResponse SendSnapshotRecoveryRequest(Server server, Peer peer, SnapshotRecoveryRequest req)
        {
            throw new NotImplementedException();
        }

        public SnapshotResponse SendSnapshotRequest(Server server, Peer peer, SnapshotRequest req)
        {
            throw new NotImplementedException();
        }

        public RequestVoteResponse SendVoteRequest(Server server, Peer peer, RequestVoteRequest req)
        {
            throw new NotImplementedException();
        }
    }
}