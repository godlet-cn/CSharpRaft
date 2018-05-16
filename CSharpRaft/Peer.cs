using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace CSharpRaft
{
    // A peer is a reference to another server involved in the consensus protocol.
    public class Peer
    {
        public string Name { get; set; }

        public string ConnectionString { get; set; }

        private Server server;

        private bool isStopped;

        private int heartbeatInterval;

        private readonly object mutex = new object();

        #region Constructor

        public Peer()
        {

        }

        public Peer(string name, string connectionString)
        {
            this.Name = name;
            this.ConnectionString = connectionString;
        }

        public Peer(Server server, string name, string connectionString, int heartbeatInterval)
        {
            this.server = server;
            this.Name = name;
            this.ConnectionString = connectionString;
            this.heartbeatInterval = heartbeatInterval;
        }

        #endregion

        #region Properties


        //--------------------------------------
        // Prev log index
        //--------------------------------------

        private int prevLogIndex;
        // Retrieves or sets the previous log index.
        [JsonIgnore]
        public int PrevLogIndex
        {
            get
            {
                return this.prevLogIndex;
            }
            set
            {
                this.prevLogIndex = value;
            }
        }

        private DateTime lastActivity;
        // LastActivity returns the last time any response was received from the peer.
        [JsonIgnore]
        public DateTime LastActivity
        {
            get
            {
                return this.lastActivity;
            }
            set
            {
                this.lastActivity = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the heartbeat timeout.
        /// </summary>
        /// <param name="duration"></param>
        public void SetHeartbeatInterval(int duration)
        {
            this.heartbeatInterval = duration;
        }

        // Clones the state of the peer. The clone is not attached to a server and
        // the heartbeat timer will not exist.
        public Peer Clone()
        {
            lock (mutex)
            {
                Peer peer = new Peer()
                {
                    Name = this.Name,
                    ConnectionString = this.ConnectionString,

                    prevLogIndex = this.prevLogIndex,
                    lastActivity = this.lastActivity
                };

                return peer;
            }
        }

        //--------------------------------------
        // Heartbeat
        //--------------------------------------

        private static Timer ticker;
        // Starts the peer heartbeat.
        internal void startHeartbeat()
        {
            this.LastActivity = DateTime.Now;

            ticker = new Timer(this.heartbeatInterval);

            // Hook up the Elapsed event for the timer. 
            ticker.Elapsed += Ticker_Elapsed;
            ticker.AutoReset = true;
            ticker.Enabled = true;

            DebugTrace.DebugLine("peer.heartbeat: ", this.Name, this.heartbeatInterval);
        }

        // Stops the peer heartbeat.
        internal void stopHeartbeat(bool flush)
        {
            this.LastActivity = DateTime.Now;
            this.isStopped = flush;
        }

        // Listens to the heartbeat timeout and flushes an AppendEntries RPC.
        private void Ticker_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this.isStopped)
            {
                // before we can safely remove a node,we must flush the remove command to the node first
                this.flush();

                DebugTrace.DebugLine("peer.heartbeat.stop.with.flush: ", this.Name);

                ticker.Stop();
                return;
            }

            DateTime start = DateTime.Now;

            this.flush();

            TimeSpan duration = DateTime.Now - start;
            this.server.DispatchHeartbeatEvent(new RaftEventArgs(duration, null));
        }

        private void flush()
        {
            try
            {
                int prevLogIndex = this.PrevLogIndex;
                int term = this.server.Term;
                List<LogEntry> entries;
                int prevLogTerm;
                this.server.log.getEntriesAfter(prevLogIndex, this.server.maxLogEntriesPerRequest, out entries, out prevLogTerm);
                if (entries != null)
                {
                    this.sendAppendEntriesRequest(new AppendEntriesRequest(term, prevLogIndex, prevLogTerm, this.server.log.CommitIndex, this.server.Name, entries));
                }
                else
                {
                    this.sendSnapshotRequest(new SnapshotRequest(this.server.Name, this.server.GetSnapshot()));
                }
            }
            catch (Exception err)
            {
                DebugTrace.DebugLine("peer.heartbeat.flush: ", err);
            }
        }
        
        // Sends an AppendEntries request to the peer through the transport.
        private void sendAppendEntriesRequest(AppendEntriesRequest req)
        {
            DebugTrace.TraceLine("==================================================================");
            DebugTrace.TraceLine(string.Format("peer.append.send: {0}->{1} [prevLog:{2} length: {3}]\n",
                this.server.Name, this.Name, req.PrevLogIndex, req.Entries.Count));

            var resp = this.server.Transporter.SendAppendEntriesRequest(this.server, this, req);

            if (resp == null||resp.Result==null)
            {
                this.server.DispatchHeartbeatIntervalEvent(new RaftEventArgs(this, null));
                DebugTrace.DebugLine("peer.append.timeout: ", this.server.Name, "->", this.Name);
                return;
            }
            DebugTrace.TraceLine("peer.append.resp: ", this.server.Name, "<-", this.Name);
            DebugTrace.TraceLine("peer.append.resp result: ", "Success:"+resp.Result.Success, "Term:"+resp.Result.Term,
                "Index:"+resp.Result.Index, "CommitIndex:" + resp.Result.CommitIndex);

            this.LastActivity = DateTime.Now;
            // If successful then update the previous log index.
            lock (mutex)
            {
                if (resp.Result.Success)
                {
                    if (req.Entries.Count > 0)
                    {
                        this.prevLogIndex = (int)req.Entries[req.Entries.Count - 1].Index;

                        // if peer append a log entry from the current term, we set append to true
                        if (req.Entries[req.Entries.Count - 1].Term == this.server.Term)
                        {
                            resp.Result.append = true;
                        }
                    }
                    DebugTrace.TraceLine("peer.append.resp.success: ", this.Name, "; idx =", this.prevLogIndex);
                    // If it was unsuccessful then decrement the previous log index and
                    // we'll try again next time.
                }
                else
                {
                    if (resp.Result.Term > this.server.Term)
                    {
                        // this happens when there is a new leader comes up that this *leader* has not
                        // known yet.
                        // this server can know until the new leader send a ae with higher term
                        // or this server finish processing this response.
                        DebugTrace.DebugLine("peer.append.resp.not.update: new.leader.found");

                    }
                    else if (resp.Result.Term == req.Term && resp.Result.CommitIndex >= this.prevLogIndex)
                    {
                        // we may miss a response from peer
                        // so maybe the peer has committed the logs we just sent
                        // but we did not receive the successful reply and did not increase
                        // the prevLogIndex

                        // peer failed to truncate the log and sent a fail reply at this time
                        // we just need to update peer's prevLog index to commitIndex

                        this.prevLogIndex = resp.Result.CommitIndex;

                        DebugTrace.DebugLine("peer.append.resp.update: ", this.Name, "; idx =", this.prevLogIndex);
                    }
                    else if (this.prevLogIndex > 0)
                    {
                        // Decrement the previous log index down until we find a match. Don't
                        // let it go below where the peer's commit index is though. That's a
                        // problem.
                        this.prevLogIndex--;
                        // if it not enough, we directly decrease to the index of the
                        if (this.prevLogIndex > resp.Result.Index)
                        {
                            this.prevLogIndex = resp.Result.Index;
                        }
                        DebugTrace.DebugLine("peer.append.resp.decrement: ", this.Name, "; idx =", this.prevLogIndex);
                    }
                }
            }

            // Attach the peer to resp, thus server can know where it comes from
            resp.Result.peer = this.Name;
            this.server.processAppendEntriesResponse(resp.Result);
        }

        // Sends an Snapshot request to the peer through the transport.
        private void sendSnapshotRequest(SnapshotRequest req)
        {
            DebugTrace.DebugLine("peer.snap.send: ", this.Name);

            var resp = this.server.Transporter.SendSnapshotRequest(this.server, this, req);

            if (resp == null)
            {
                DebugTrace.DebugLine("peer.snap.timeout: ", this.Name);
                return;
            }

            DebugTrace.DebugLine("peer.snap.recv: ", this.Name);

            //If successful, the peer should have been to snapshot state
            //Send it the snapshot!
            this.LastActivity = DateTime.Now;

            if (resp.Result.Success)
            {
                this.sendSnapshotRecoveryRequest();
            }
            else
            {
                DebugTrace.DebugLine("peer.snap.failed: ", this.Name);
                return;
            }
        }

        // Sends an Snapshot Recovery request to the peer through the transport.
        private void sendSnapshotRecoveryRequest()
        {
            SnapshotRecoveryRequest req = new SnapshotRecoveryRequest(this.server.Name, this.server.GetSnapshot());

            DebugTrace.DebugLine("peer.snap.recovery.send: ", this.Name);

            var resp = this.server.Transporter.SendSnapshotRecoveryRequest(this.server, this, req);

            if (resp == null)
            {
                DebugTrace.DebugLine("peer.snap.recovery.timeout: ", this.Name);
                return;
            }

            this.LastActivity = DateTime.Now;

            if (resp.Result.Success)
            {
                this.prevLogIndex = req.LastIndex;
            }
            else
            {
                DebugTrace.DebugLine("peer.snap.recovery.failed: ", this.Name);
                return;
            }
        }

        //--------------------------------------
        // Vote Requests
        //--------------------------------------
        // send VoteRequest Request
        internal Task<RequestVoteResponse> sendVoteRequest(RequestVoteRequest req)
        {
            DebugTrace.DebugLine("peer.vote: ", this.server.Name, "->", this.Name);
            req.peer = this;
            var resp = this.server.Transporter.SendVoteRequest(this.server, this, req);
            if (resp != null)
            {
                DebugTrace.DebugLine("peer.vote.recv: ", this.server.Name, "<-", this.Name);
                this.LastActivity = DateTime.Now;
                resp.Result.peer = this;
            }
            else
            {
                DebugTrace.DebugLine("peer.vote.failed: ", this.server.Name, "<-", this.Name);
                return null;
            }
            return resp;
        }
        
        #endregion
    }
}