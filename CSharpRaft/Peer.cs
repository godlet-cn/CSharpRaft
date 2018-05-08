using System;
using System.Collections.Generic;

namespace CSharpRaft
{
    // A peer is a reference to another server involved in the consensus protocol.
    public class Peer
    {
        public string Name { get; set; }

        public string ConnectionString { get; set; }

        internal Server server;


        internal bool stopChan;

        internal int heartbeatInterval;


        private object mutex;

        //------------------------------------------------------------------------------
        //
        // Constructor
        //
        //------------------------------------------------------------------------------
        public Peer()
        {

        }

        public Peer(Server server, string name, string connectionString, int heartbeatInterval)
        {
            this.server = server;
            this.Name = name;
            this.ConnectionString = connectionString;
            this.heartbeatInterval = heartbeatInterval;
        }

        //------------------------------------------------------------------------------
        //
        // Accessors
        //
        //------------------------------------------------------------------------------

        // Sets the heartbeat timeout.
        public void setHeartbeatInterval(int duration)
        {
            this.heartbeatInterval = duration;
        }

        //--------------------------------------
        // Prev log index
        //--------------------------------------

        private int prevLogIndex;
        // Retrieves or sets the previous log index.
        public int PrevLogIndex
        {
            get
            {
                lock (mutex)
                {
                    return this.prevLogIndex;
                }
            }
            set
            {
                lock (mutex)
                {
                    this.prevLogIndex = value;
                }
            }
        }

        private DateTime lastActivity;
        // LastActivity returns the last time any response was received from the peer.
        public DateTime LastActivity
        {
            get
            {
                lock (mutex)
                {
                    return this.lastActivity;
                }
            }
            set
            {
                lock (mutex)
                {
                    this.lastActivity = value;
                }
            }
        }

        //------------------------------------------------------------------------------
        //
        // Methods
        //
        //------------------------------------------------------------------------------

        //--------------------------------------
        // Copying
        //--------------------------------------

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

        // Starts the peer heartbeat.
        public void startHeartbeat()
        {
            //this.stopChan = make(chan bool);

            //c:= make(chan bool);

            this.LastActivity = DateTime.Now;

            //this.server.routineGroup.Add(1)

            //go func()
            //{
            //    defer this.server.routineGroup.Done()

            //    this.heartbeat(c)

            //} ()
            // < -c
        }

        // Stops the peer heartbeat.
        public void stopHeartbeat(bool flush)
        {
            this.LastActivity = DateTime.Now;
            this.stopChan = flush;
        }

        //--------------------------------------
        // Heartbeat
        //--------------------------------------

        // Listens to the heartbeat timeout and flushes an AppendEntries RPC.
        public void heartbeat(bool c)
        {
            //          stopChan:= this.stopChan;


            //          c < -true;


            //          ticker:= time.Tick(this.heartbeatInterval);


            //          DebugTrace.Debug("peer.heartbeat: ", this.Name, this.heartbeatInterval);

            //  for {
            //      select {
            //case flush:= < -stopChan:
            //	if flush {
            //              // before we can safely remove a node
            //              // we must flush the remove command to the node first
            //              this.flush()

            //              DebugTrace.Debug("peer.heartbeat.stop.with.flush: ", this.Name)

            //              return

            //          }
            //          else
            //          {
            //              DebugTrace.Debug("peer.heartbeat.stop: ", this.Name)

            //              return

            //          }

            //case < -ticker:
            //	start:= time.Now()

            //          this.flush()

            //          duration:= time.Now().Sub(start)

            //          this.server.DispatchEvent(newEvent(HeartbeatEventType, duration, null))

            //      }
            //}
        }

        public void flush()
        {
            DebugTrace.Debug("peer.heartbeat.flush: ", this.Name);

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

        //--------------------------------------
        // Append Entries
        //--------------------------------------

        // Sends an AppendEntries request to the peer through the transport.
        public void sendAppendEntriesRequest(AppendEntriesRequest req)
        {
            DebugTrace.Trace("peer.append.send: {0}->{1} [prevLog:{2} length: {3}]\n",
                this.server.Name, this.Name, req.PrevLogIndex, req.Entries.Count);


            var resp = this.server.Transporter.SendAppendEntriesRequest(this.server, this, req);

            if (resp == null)
            {
                this.server.DispatchHeartbeatIntervalEvent(new RaftEventArgs(this, null));
                DebugTrace.Debug("peer.append.timeout: ", this.server.Name, "->", this.Name);

                return;
            }
            DebugTrace.TraceLine("peer.append.resp: ", this.server.Name, "<-", this.Name);


            this.LastActivity = DateTime.Now;
            // If successful then update the previous log index.
            lock (mutex)
            {

                if (resp.Success)
                {
                    if (req.Entries.Count > 0)
                    {
                        this.prevLogIndex = (int)req.Entries[req.Entries.Count - 1].Index;

                        // if peer append a log entry from the current term
                        // we set append to true
                        if (req.Entries[req.Entries.Count - 1].Term == this.server.Term)
                        {
                            resp.append = true;
                        }
                    }
                    DebugTrace.TraceLine("peer.append.resp.success: ", this.Name, "; idx =", this.prevLogIndex);
                    // If it was unsuccessful then decrement the previous log index and
                    // we'll try again next time.
                }
                else
                {
                    if (resp.Term > this.server.Term)
                    {
                        // this happens when there is a new leader comes up that this *leader* has not
                        // known yet.
                        // this server can know until the new leader send a ae with higher term
                        // or this server finish processing this response.
                        DebugTrace.Debug("peer.append.resp.not.update: new.leader.found");

                    }
                    else if (resp.Term == req.Term && resp.CommitIndex >= this.prevLogIndex)
                    {
                        // we may miss a response from peer
                        // so maybe the peer has committed the logs we just sent
                        // but we did not receive the successful reply and did not increase
                        // the prevLogIndex

                        // peer failed to truncate the log and sent a fail reply at this time
                        // we just need to update peer's prevLog index to commitIndex

                        this.prevLogIndex = resp.CommitIndex;

                        DebugTrace.Debug("peer.append.resp.update: ", this.Name, "; idx =", this.prevLogIndex);
                    }
                    else if (this.prevLogIndex > 0)
                    {
                        // Decrement the previous log index down until we find a match. Don't
                        // let it go below where the peer's commit index is though. That's a
                        // problem.
                        this.prevLogIndex--;
                        // if it not enough, we directly decrease to the index of the
                        if (this.prevLogIndex > resp.Index)
                        {
                            this.prevLogIndex = resp.Index;

                        };

                        DebugTrace.Debug("peer.append.resp.decrement: ", this.Name, "; idx =", this.prevLogIndex);

                    }
                }
            }

            // Attach the peer to resp, thus server can know where it comes from
            resp.peer = this.Name;
            // Send response to server for processing.
            this.server.sendAsync(resp);
        }

        // Sends an Snapshot request to the peer through the transport.
        public void sendSnapshotRequest(SnapshotRequest req)
        {
            DebugTrace.Debug("peer.snap.send: ", this.Name);

            var resp = this.server.Transporter.SendSnapshotRequest(this.server, this, req);

            if (resp == null)
            {
                DebugTrace.Debug("peer.snap.timeout: ", this.Name);
                return;
            }

            DebugTrace.Debug("peer.snap.recv: ", this.Name);

            //If successful, the peer should have been to snapshot state
            //Send it the snapshot!
            this.LastActivity = DateTime.Now;
            if (resp.Success)
            {
                this.sendSnapshotRecoveryRequest();
            }
            else
            {
                DebugTrace.Debug("peer.snap.failed: ", this.Name);
                return;
            }
        }

        // Sends an Snapshot Recovery request to the peer through the transport.
        public void sendSnapshotRecoveryRequest()
        {
            SnapshotRecoveryRequest req = new SnapshotRecoveryRequest(this.server.Name, this.server.GetSnapshot());

            DebugTrace.Debug("peer.snap.recovery.send: ", this.Name);

            var resp = this.server.Transporter.SendSnapshotRecoveryRequest(this.server, this, req);

            if (resp == null)
            {
                DebugTrace.Debug("peer.snap.recovery.timeout: ", this.Name);
                return;
            }

            this.LastActivity = DateTime.Now;

            if (resp.Success)
            {
                this.prevLogIndex = req.LastIndex;
            }
            else
            {
                DebugTrace.Debug("peer.snap.recovery.failed: ", this.Name);
                return;
            }
            this.server.sendAsync(resp);
        }

        //--------------------------------------
        // Vote Requests
        //--------------------------------------
        // send VoteRequest Request
        public void sendVoteRequest(RequestVoteRequest req, RequestVoteResponse c)
        {
            DebugTrace.Debug("peer.vote: ", this.server.Name, "->", this.Name);

            req.peer = this;

            RequestVoteResponse resp = this.server.Transporter.SendVoteRequest(this.server, this, req);
            if (resp != null)
            {
                DebugTrace.Debug("peer.vote.recv: ", this.server.Name, "<-", this.Name);

                this.LastActivity = DateTime.Now;

                resp.peer = this;

                c = resp;
            }
            else
            {
                DebugTrace.Debug("peer.vote.failed: ", this.server.Name, "<-", this.Name);
            }
        }
    }
}