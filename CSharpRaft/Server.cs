using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace CSharpRaft
{
    public class Server : IServer
    {
        internal string name;
        internal string path;
        internal ServerState state;
        internal Transporter transporter;

        internal object context;
        internal int currentTerm;

        internal object mutex;
        internal string votedFor;

        internal Log log;

        internal bool stopped;

        internal string leader;

        internal Dictionary<string, Peer> peers;

        internal Dictionary<string, bool> syncedPeer;

        internal int electionTimeout;

        internal int heartbeatInterval;

        public Snapshot snapshot;

        // PendingSnapshot is an unfinished snapshot.
        // After the pendingSnapshot is saved to disk,
        // it will be set to snapshot and also will be
        // set to null.
        internal Snapshot pendingSnapshot;

        public StateMachine stateMachine;
        internal int maxLogEntriesPerRequest;

        internal string connectionString;

        public event RaftEventHandler StateChanged;
        public event RaftEventHandler LeaderChanged;
        public event RaftEventHandler TermChanged;
        public event RaftEventHandler Commited;
        public event RaftEventHandler PeerAdded;
        public event RaftEventHandler PeerRemoved;
        public event RaftEventHandler HeartbeatIntervalReached;
        public event RaftEventHandler ElectionTimeoutThresholdReached;
        public event RaftEventHandler HeartbeatReached;

        public void DispatchStateChangeEvent(RaftEventArgs args) {
            if (StateChanged != null)
            {
                StateChanged(this, args);
            }
        }
        public void DispatchLeaderChangeEvent(RaftEventArgs args)
        {
            if (LeaderChanged != null)
            {
                LeaderChanged(this, args);
            }
        }
        public void DispatchTermChangeEvent(RaftEventArgs args)
        {
            if (TermChanged != null)
            {
                TermChanged(this, args);
            }
        }
        public void DispatchCommiteEvent(RaftEventArgs args)
        {
            if (Commited != null)
            {
                Commited(this, args);
            }
        }
        public void DispatchAddPeerEvent(RaftEventArgs args)
        {
            if (PeerAdded != null)
            {
                PeerAdded(this, args);
            }
        }
        public void DispatchRemovePeerEvent(RaftEventArgs args)
        {
            if (PeerRemoved != null)
            {
                PeerRemoved(this, args);
            }
        }
        public void DispatchHeartbeatIntervalEvent(RaftEventArgs args)
        {
            if (HeartbeatIntervalReached != null)
            {
                HeartbeatIntervalReached(this, args);
            }
        }
        public void DispatchElectionTimeoutThresholdEvent(RaftEventArgs args)
        {
            if (ElectionTimeoutThresholdReached != null)
            {
                ElectionTimeoutThresholdReached(this, args);
            }
        }

        public void DispatchHeartbeatEvent(RaftEventArgs args)
        {
            if (HeartbeatReached != null)
            {
                HeartbeatReached(this, args);
            }
        }
        //------------------------------------------------------------------------------
        //
        // Constructor
        //
        //------------------------------------------------------------------------------

        // Creates a new server with a log at the given path. transporter must
        // not be null. stateMachine can be null if snapshotting and log
        // compaction is to be disabled. context can be anything (including null)
        // and is not used by the raft package except returned by
        // Server.Context(). connectionString can be anything.
        public Server(string name, string path, Transporter transporter, StateMachine stateMachine, object ctx, string connectionString)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("raft.Server: Name cannot be blank");

            }
            if (transporter == null)
            {
                throw new ArgumentNullException("raft: Transporter required");

            }
            mutex = new object();

            this.name = name;
            this.path = path;
            this.transporter = transporter;
            this.stateMachine = stateMachine;
            this.context = ctx;
            this.state = ServerState.Stopped;
            this.peers = new Dictionary<string, Peer>();
            this.log = new Log();

            this.electionTimeout = Constants.DefaultElectionTimeout;
            this.heartbeatInterval = Constants.DefaultHeartbeatInterval;
            this.maxLogEntriesPerRequest = Constants.MaxLogEntriesPerRequest;
            this.connectionString = connectionString;

            this.log.ApplyFunc = (LogEntry e, Command c) =>
            {
                if (Commited != null)
                {
                    Commited(this, new RaftEventArgs(e, null));
                }
                if (c is CommandApply)
                {
                    CommandApply applyCmd = c as CommandApply;
                    applyCmd.Apply(new context()
                    {
                        server = this,
                        currentTerm = this.currentTerm,
                        currentIndex = this.log.internalCurrentIndex(),
                        commitIndex = this.log.commitIndex,
                    });
                }
                return this;
            };
        }


        //------------------------------------------------------------------------------
        //
        // Accessors
        //
        //------------------------------------------------------------------------------

        //--------------------------------------
        // General
        //--------------------------------------

        // Retrieves the name of the server.
        public string Name()
        {
            return this.name;
        }

        // Retrieves the storage path for the server.
        public string GetPath()
        {
            return this.path;
        }

        // The name of the current leader.
        public string Leader()
        {
            return this.leader;
        }

        // Retrieves a copy of the peer data.
        public Dictionary<string, Peer> Peers()
        {
            lock (mutex)
            {
                Dictionary<string, Peer> peers = new Dictionary<string, Peer>();
                foreach (var peerItem in this.peers)
                {
                    peers[peerItem.Key] = peerItem.Value.Clone();
                }
                return peers;
            }
        }

        // Retrieves the object that transports requests.
        public Transporter Transporter()
        {
            lock (mutex)
            {
                return this.transporter;
            }
        }

        public void SetTransporter(Transporter t)
        {
            lock (mutex)
            {
                this.transporter = t;
            }
        }

        // Retrieves the context passed into the constructor.
        public object Context()
        {
            return this.context;
        }

        // Retrieves the state machine passed into the constructor.
        public StateMachine StateMachine()
        {
            return this.stateMachine;
        }

        // Retrieves the log path for the server.
        public string LogPath()
        {
            return System.IO.Path.Combine(this.path, "log");
        }

        // Retrieves the current state of the server.
        public ServerState State()
        {
            lock (mutex)
            {
                return this.state;
            }
        }

        // Sets the state of the server.
        public void setState(ServerState state)
        {
            lock (mutex)
            {
                // Temporarily store previous values.
                ServerState prevState = this.state;

                string prevLeader = this.leader;

                // Update state and leader.
                this.state = state;

                if (state == ServerState.Leader)
                {
                    this.leader = this.Name();

                    this.syncedPeer = new Dictionary<string, bool>();
                }

                // Dispatch state and leader change events.
                if (StateChanged != null)
                {
                    StateChanged(this, new RaftEventArgs(this.state, prevState));
                }

                if (prevLeader != this.leader)
                {
                    if (LeaderChanged != null)
                    {
                        LeaderChanged(this, new RaftEventArgs(this.leader, prevLeader));
                    }
                }
            }
        }

        // Retrieves the current term of the server.
        public int Term()
        {
            lock (mutex)
            {
                return this.currentTerm;
            }
        }

        // Retrieves the current commit index of the server.
        public int CommitIndex()
        {
            lock (this.log.mutex)
            {
                return this.log.commitIndex;
            }
        }

        // Retrieves the name of the candidate this server voted for in this term.
        public string VotedFor()
        {
            return this.votedFor;
        }

        // Retrieves whether the server's log has no entries.
        public bool IsLogEmpty()
        {
            return this.log.isEmpty();
        }

        // A list of all the log entries. This should only be used for debugging purposes.
        public List<LogEntry> LogEntries()
        {
            lock (this.log.mutex)
            {
                return this.log.entries;
            }
        }

        // A reference to the command name of the last entry.
        public string LastCommandName()
        {
            return this.log.lastCommandName();
        }

        // Get the state of the server for debugging
        public string GetState()
        {
            lock (mutex)
            {
                return string.Format("Name: %s, State: %s, Term: %d, CommitedIndex: %d ", this.name, this.state, this.currentTerm, this.log.commitIndex);
            }
        }

        // Check if the server is promotable
        public bool promotable()
        {
            return this.log.currentIndex() > 0;
        }

        //--------------------------------------
        // Membership
        //--------------------------------------

        // Retrieves the number of member servers in the consensus.
        public int MemberCount()
        {
            lock (mutex)
            {
                return this.peers.Count + 1;
            }
        }

        // Retrieves the number of servers required to make a quorum.
        public int QuorumSize()
        {
            return (this.MemberCount() / 2) + 1;
        }

        //--------------------------------------
        // Election timeout
        //--------------------------------------

        // Retrieves the election timeout.
        public int ElectionTimeout()
        {
            lock (mutex)
            {
                return this.electionTimeout;
            }
        }

        // Sets the election timeout.
        public void SetElectionTimeout(int duration)
        {
            lock (mutex)
            {

                this.electionTimeout = duration;
            }
        }

        //--------------------------------------
        // Heartbeat timeout
        //--------------------------------------

        // Retrieves the heartbeat timeout.
        public int HeartbeatInterval()
        {
            lock (mutex)
            {
                return this.heartbeatInterval;
            }
        }

        // Sets the heartbeat timeout.
        public void SetHeartbeatInterval(int duration)
        {
            lock (mutex)
            {
                this.heartbeatInterval = duration;

                foreach (var peer in this.peers)
                {
                    peer.Value.setHeartbeatInterval(duration);
                }
            }
        }
        
        // Checks if the server is currently running.
        public bool Running()
        {
            lock (mutex)
            {
                return (this.state != ServerState.Stopped && this.state != ServerState.Initialized);
            }
        }

        // Init initializes the raft server.
        // If there is no previous log file under the given path, Init() will create an empty log file.
        // Otherwise, Init() will load in the log entries from the log file.
        public void Init()
        {
            if (this.Running())
            {
                Console.Error.WriteLine(string.Format("raft.Server: Server already running[%s]", this.state));
                return;
            }

            // Server has been initialized or server was stopped after initialized
            // If log has been initialized, we know that the server was stopped after
            // running.
            if (this.state == ServerState.Initialized || this.log.initialized)
            {
                this.state = ServerState.Initialized;
                return;

            }
            string snapshotDir = Path.Combine(this.path, "snapshot");

            try
            {
                // Create snapshot directory if it does not exist
                if (!Directory.Exists(snapshotDir))
                {
                    Directory.CreateDirectory(snapshotDir);
                }
            }
            catch (Exception ex)
            {
                DebugTrace.DebugLine("raft: Snapshot directory doesn't exist");
                Console.Error.WriteLine(string.Format("raft: Initialization error: %s", ex));
                return;
            }
            
            try
            {
                this.readConf();
            }
            catch (Exception err)
            {
                DebugTrace.DebugLine("raft: Conf file error: ", err);
                Console.Error.WriteLine(string.Format("raft: Initialization error: %s", err));
                return;
            }

            try
            {
                // Initialize the log and load it up.
                this.log.open(this.LogPath());
            }
            catch (Exception err)
            {
                DebugTrace.DebugLine("raft: Log error: ", err);
                Console.Error.WriteLine(string.Format("raft: Initialization error: %s", err));
                return;
            }

            // Update the term to the last term in the log.
            int index, curTerm;
            this.log.lastInfo(out index, out curTerm);
            this.currentTerm = curTerm;

            this.state = ServerState.Initialized;
        }

        // Start the raft server
        // If log entries exist then allow promotion to candidate if no AEs received.
        // If no log entries exist then wait for AEs from another node.
        // If no log entries exist and a self-join command is issued then
        // immediately become leader and commit entry.
        public void Start()
        {
            // Exit if the server is already running.
            if (this.Running())
            {
                throw new Exception(string.Format("raft.Server: Server already running[%s]", this.state));
            }
            this.Init();

            // stopped needs to be allocated each time server starts
            // because it is closed at `Stop`.

            //this.stopped = make(chan bool);

            this.setState(ServerState.Follower);

            // If no log entries exist then
            // 1. wait for AEs from another node
            // 2. wait for self-join command
            // to set itself promotable
            if (!this.promotable())
            {
                DebugTrace.DebugLine("start as a new raft server");

                // If log entries exist then allow promotion to candidate
                // if no AEs received.
            }
            else
            {
                DebugTrace.DebugLine("start from previous saved state");
            }

            DebugTrace.DebugLine(this.GetState());

            Task.Factory.StartNew(() => { this.loop(); });
        }


        // Shuts down the server.
        public void Stop()
        {
            if (this.State() == ServerState.Stopped)
            {
                return;

            }

            //TODO
            //close(this.stopped);

            //// make sure all goroutines have stopped before we close the log
            //this.routineGroup.Wait();

            this.log.close();

            this.setState(ServerState.Stopped);
        }


        //--------------------------------------
        // Term
        //--------------------------------------

        // updates the current term for the server. This is only used when a larger
        // external term is found.
        public void updateCurrentTerm(int term, string leaderName)
        {
            if (term < this.currentTerm)
            {
                throw new Exception("upadteCurrentTerm: update is called when term is not larger than currentTerm");
            }

            // Store previous values temporarily.
            int prevTerm = this.currentTerm;

            string prevLeader = this.leader;

            // set currentTerm = T, convert to follower (§5.1)
            // stop heartbeats before step-down
            if (this.state == ServerState.Leader)
            {
                foreach (var peeritem in this.peers)
                {
                    peeritem.Value.stopHeartbeat(false);
                }
            }
            // update the term and clear vote for
            if (this.state != ServerState.Follower)
            {
                this.setState(ServerState.Follower);

            }

            lock (mutex)
            {
                this.currentTerm = term;
                this.leader = leaderName;
                this.votedFor = "";
            }
            // Dispatch change events.
            if (TermChanged != null)
            {
                TermChanged(this, new RaftEventArgs(this.currentTerm, prevTerm));
            }

            if (prevLeader != this.leader)
            {
                if (LeaderChanged != null)
                {
                    LeaderChanged(this, new RaftEventArgs(this.leader, prevLeader));
                }
            }
        }

        //--------------------------------------
        // Event Loop
        //--------------------------------------

        //               ________
        //            --|Snapshot|                 timeout
        //            |  --------                  ______
        // recover    |       ^                   |      |
        // snapshot / |       |snapshot           |      |
        // higher     |       |                   v      |     recv majority votes
        // term       |    --------    timeout    -----------                        -----------
        //            |-> |Follower| ----------> | Candidate |--------------------> |  Leader   |
        //                 --------               -----------                        -----------
        //                    ^          higher term/ |                         higher term |
        //                    |            new leader |                                     |
        //                    |_______________________|____________________________________ |
        // The main event loop for the server
        public void loop()
        {
            try
            {
                ServerState state = this.State();

                while (state != ServerState.Stopped)
                {
                    DebugTrace.DebugLine("server.loop.run ", state);


                    switch (state)
                    {
                        case ServerState.Follower:
                            this.followerLoop();
                            break;
                        case ServerState.Candidate:
                            this.candidateLoop();
                            break;
                        case ServerState.Leader:
                            this.leaderLoop();
                            break;
                        case ServerState.Snapshotting:
                            this.snapshotLoop();
                            break;
                    }
                    state = this.State();
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                DebugTrace.DebugLine("server.loop.end");
            }
        }

        // Sends an event to the event loop to be processed. The function will wait
        // until the event is actually processed before returning.
        public object send(object value)
        {
            if (!this.Running())
            {
                throw Constants.StopError;
            }

            //event = &ev { target: value, c: make(chan error, 1)}
            //select
            //{
            //	case this.c < - event:
            //	case < -this.stopped:
            //		return null, StopError

            //}
            //select
            //{
            //	case < -this.stopped:
            //		return null, StopError
            //	case err:= < -event.c:
            //    return event.returnValue, err

            //}

            return null;
        }

        public void sendAsync(object value)
        {
            if (!this.Running())
            {
                return;
            }

            //event := &ev { target: value, c: make(chan error, 1)}
            //// try a non-blocking send first
            //// in most cases, this should not be blocking
            //// avoid create unnecessary go routines
            //select
            //{
            //    	case this.c < - event:
            //    		return
            //        default:
            //    	}

            //    this.routineGroup.Add(1)
            //    	go func() {
            //    defer this.routineGroup.Done()

            //            select {
            //    		case this.c < - event:
            //    		case < -this.stopped:
            //    		}
            //}()
        }

        // The event loop that is run when the server is in a Follower state.
        // Responds to RPCs from candidates and leaders.
        // Converts to candidate if election timeout elapses without either:
        //   1.Receiving valid AppendEntries RPC, or
        //   2.Granting vote to candidate
        public void followerLoop()
        {
            DateTime since = DateTime.Now;
            
            int electionTimeout = this.ElectionTimeout();

            //  timeoutChan:= afterBetween(this.ElectionTimeout(), this.ElectionTimeout() * 2)


            //  for (this.State() == Follower) {
            //          var err error
            //          update := false

            //      select {
            //case < -this.stopped:
            //	this.setState(Stopped)

            //          return

            //case e:= < -this.c:
            //	switch req := e.target.(type) {
            //	case JoinCommand:
            //                  //If no log entries exist and a self-join command is issued
            //                  //then immediately become leader and commit entry.
            //                  if this.log.currentIndex() == 0 && req.NodeName() == this.Name() {
            //                      DebugTrace.DebugLine("selfjoin and promote to leader")

            //                  this.setState(Leader)

            //                  this.processCommand(req, e)

            //              }
            //                  else
            //                  {
            //                      err = NotLeaderError

            //              }
            //	case *AppendEntriesRequest:
            //		// If heartbeats get too close to the election timeout then send an event.
            //		elapsedTime:= time.Now().Sub(since)

            //              if elapsedTime > time.Duration(float64(electionTimeout) * ElectionTimeoutThresholdPercent) {
            //                      this.DispatchEvent(newEvent(ElectionTimeoutThresholdEventType, elapsedTime, null))

            //              }
            //                  e.returnValue, update = this.processAppendEntriesRequest(req)
            //	case *RequestVoteRequest:
            //		e.returnValue, update = this.processRequestVoteRequest(req)
            //	case *SnapshotRequest:
            //		e.returnValue = this.processSnapshotRequest(req)

            //          default:
            //		err = NotLeaderError

            //          }
            //              // Callback to event.
            //              e.c < -err

            //case < -timeoutChan:
            //	// only allow synced follower to promote to candidate
            //	if this.promotable() {
            //                  this.setState(Candidate)

            //          }
            //              else
            //              {
            //                  update = true

            //          }
            //          }

            //          // Converts to candidate if election timeout elapses without either:
            //          //   1.Receiving valid AppendEntries RPC, or
            //          //   2.Granting vote to candidate
            //          if update {
            //              since = time.Now()

            //          timeoutChan = afterBetween(this.ElectionTimeout(), this.ElectionTimeout() * 2)

            //      }
            //      }
        }

        // The event loop that is run when the server is in a Candidate state.
        public void candidateLoop()
        {
            // Clear leader value.
            string prevLeader = this.leader;
            this.leader = "";

            if (prevLeader != this.leader)
            {
                if (LeaderChanged != null)
                {
                    LeaderChanged(this, new RaftEventArgs(this.leader, prevLeader));
                }
            }

            int lastLogIndex, lastLogTerm;
            this.log.lastInfo(out lastLogIndex, out lastLogTerm);


            //  doVote:= true

            //  votesGranted:= 0

            //  var timeoutChan<-chan time.Time
            // var respChan chan *RequestVoteResponse


            //  for this.State() == Candidate {
            //          if doVote {
            //              // Increment current term, vote for self.
            //              this.currentTerm++

            //          this.votedFor = this.name

            //          // Send RequestVote RPCs to all other servers.
            //              respChan = make(chan * RequestVoteResponse, len(this.peers))

            //          for _, peer := range this.peers {
            //                  this.routineGroup.Add(1)

            //              go func(peer* Peer)
            //                  {
            //                      defer this.routineGroup.Done()

            //                  peer.sendVoteRequest(newRequestVoteRequest(this.currentTerm, this.name, lastLogIndex, lastLogTerm), respChan)

            //              } (peer)

            //          }

            //              // Wait for either:
            //              //   * Votes received from majority of servers: become leader
            //              //   * AppendEntries RPC received from new leader: step down.
            //              //   * Election timeout elapses without election resolution: increment term, start new election
            //              //   * Discover higher term: step down (§5.1)
            //              votesGranted = 1

            //          timeoutChan = afterBetween(this.ElectionTimeout(), this.ElectionTimeout() * 2)

            //          doVote = false

            //      }

            //          // If we received enough votes then stop waiting for more votes.
            //          // And return from the candidate loop
            //          if votesGranted == this.QuorumSize() {
            //              DebugTrace.DebugLine("server.candidate.recv.enough.votes")

            //          this.setState(Leader)

            //          return

            //      }

            //          // Collect votes from peers.
            //          select {
            //case < -this.stopped:
            //	this.setState(Stopped)

            //          return

            //case resp:= < -respChan:
            //	if success := this.processVoteResponse(resp); success {
            //                  DebugTrace.DebugLine("server.candidate.vote.granted: ", votesGranted)

            //              votesGranted++

            //          }

            //case e:= < -this.c:
            //	var err error

            //          switch req := e.target.(type) {
            //	case Command:
            //                  err = NotLeaderError
            //	case *AppendEntriesRequest:
            //		e.returnValue, _ = this.processAppendEntriesRequest(req)
            //	case *RequestVoteRequest:
            //		e.returnValue, _ = this.processRequestVoteRequest(req)

            //          }

            //              // Callback to event.
            //              e.c < -err

            //case < -timeoutChan:
            //	doVote = true

            //      }
            //      }
        }

        // The event loop that is run when the server is in a Leader state.
        public void leaderLoop()
        {
            int logIndex, logTerm;
            this.log.lastInfo(out logIndex, out logTerm);

            // Update the peers prevLogIndex to leader's lastLogIndex and start heartbeat.
            DebugTrace.DebugLine("leaderLoop.set.PrevIndex to ", logIndex);

            foreach (var peerItem in this.peers)
            {
                peerItem.Value.setPrevLogIndex(logIndex);
                peerItem.Value.startHeartbeat();
            }

            // Commit a NOP after the server becomes leader. From the Raft paper:
            // "Upon election: send initial empty AppendEntries RPCs (heartbeat) to
            // each server; repeat during idle periods to prevent election timeouts
            // (§5.2)". The heartbeats started above do the "idle" period work.

            Task.Factory.StartNew(new Action(() => {
                this.Do(new NOPCommand { });
            }));

            // Begin to collect response from followers
            while (this.State() == ServerState.Leader)
            {
                if (this.stopped) {
                    // Stop all peers before stop
                    foreach (var peer in this.peers)
                    {
                        peer.Value.stopHeartbeat(false);
                    }
                    this.setState(ServerState.Stopped);
                    return;
                }

                //        var err error
                //        select
                //{
                //		case e := <-this.c:
                //			switch req := e.target.(type) {
                //			case Command:
                //        this.processCommand(req, e)

                //                continue
                //			case *AppendEntriesRequest:

                //                e.returnValue, _ = this.processAppendEntriesRequest(req)
                //			case *AppendEntriesResponse:
                //				this.processAppendEntriesResponse(req)
                //			case *RequestVoteRequest:

                //                e.returnValue, _ = this.processRequestVoteRequest(req)

                //            }

                //    // Callback to event.
                //    e.c<- err
                //}
            }
            this.syncedPeer = null;
        }

        public void snapshotLoop()
        {
            //  for this.State() == Snapshotting {
            //      var err error
            //      select {
            //case < -this.stopped:
            //	this.setState(Stopped)

            //          return

            //case e:= < -this.c:
            //	switch req := e.target.(type) {
            //	case Command:
            //              err = NotLeaderError
            //	case *AppendEntriesRequest:
            //		e.returnValue, _ = this.processAppendEntriesRequest(req)
            //	case *RequestVoteRequest:
            //		e.returnValue, _ = this.processRequestVoteRequest(req)
            //	case *SnapshotRecoveryRequest:
            //		e.returnValue = this.processSnapshotRecoveryRequest(req)

            //          }
            //          // Callback to event.
            //          e.c < -err

            //      }
            //  }
        }

        //--------------------------------------
        // Commands
        //--------------------------------------

        // Attempts to execute a command and replicate it. The function will return
        // when the command has been successfully committed or an error has occurred.
        public object Do(Command command)
        {
            return this.send(command);
        }

        // Processes a command.
        public void processCommand(Command command, LogEvent ev)
        {
            DebugTrace.DebugLine("server.command.process");

            try
            {
                // Create an entry for the command in the log.
                var entry = this.log.createEntry(this.currentTerm, command, ev);

                this.log.appendEntry(entry);
                this.syncedPeer[this.Name()] = true;

                if (this.peers.Count == 0)
                {
                    int commitIndex = this.log.currentIndex();

                    this.log.setCommitIndex(commitIndex);

                    DebugTrace.DebugLine("commit index ", commitIndex);
                }
            }
            catch (Exception err)
            {
                DebugTrace.DebugLine("server.command.log.entry.error:", err);
                ev.c = err;
            }
        }

        //--------------------------------------
        // Append Entries
        //--------------------------------------

        // Appends zero or more log entry from the leader to this server.
        public AppendEntriesResponse AppendEntries(AppendEntriesRequest req)
        {
            //   ret, _ := this.send(req)
            //resp, _ := ret.(*AppendEntriesResponse)
            //return resp
            return null;
        }

        // Processes the "append entries" request.
        public AppendEntriesResponse processAppendEntriesRequest(AppendEntriesRequest req)
        {
            this.TraceLine("server.ae.process");


                if (req.Term < this.currentTerm)
            {
                DebugTrace.DebugLine("server.ae.error: stale term");

                    return new AppendEntriesResponse(this.currentTerm, false, this.log.currentIndex(), this.log.CommitIndex());

                }

            if (req.Term == this.currentTerm)
            {
                if (this.State() == ServerState.Leader) {
                    throw new Exception(string.Format("leader.elected.at.same.term.%d\n", this.currentTerm));
                }
                    // step-down to follower when it is a candidate
                if (this.state == ServerState.Candidate) {
                    // change state to follower
                    this.setState(ServerState.Follower);
                }

                // discover new leader when candidate
                // save leader name when follower
                this.leader = req.LeaderName;
                }
            else
            {
                // Update term and leader.
                this.updateCurrentTerm(req.Term, req.LeaderName);

                }

            // Reject if log doesn't contain a matching previous entry.
            try
            {
                this.log.truncate(req.PrevLogIndex, req.PrevLogTerm);
            }
            catch (Exception err)
            {
                DebugTrace.DebugLine("server.ae.truncate.error: ", err);
                return new AppendEntriesResponse(this.currentTerm, false, this.log.currentIndex(), this.log.CommitIndex());
            }

            // Append entries to the log.
            try
            {
                this.log.appendEntries(req.Entries);
            }
            catch (Exception err)
            {
                DebugTrace.DebugLine("server.ae.append.error: ", err);
                    return new AppendEntriesResponse(this.currentTerm, false, this.log.currentIndex(), this.log.CommitIndex());
            }

            try
            {
                // Commit up to the commit index.
                this.log.setCommitIndex(req.CommitIndex);
            }
            catch (Exception err)
            {
                DebugTrace.DebugLine("server.ae.commit.error: ", err);

                    return new AppendEntriesResponse(this.currentTerm, false, this.log.currentIndex(), this.log.CommitIndex());
            }

            // once the server appended and committed all the log entries from the leader
            return new AppendEntriesResponse(this.currentTerm, true, this.log.currentIndex(), this.log.CommitIndex());
        }

        // Processes the "append entries" response from the peer. This is only
        // processed when the server is a leader. Responses received during other
        // states are dropped.
        public void processAppendEntriesResponse(AppendEntriesResponse resp)
        {
            // If we find a higher term then change to a follower and exit.
            if (resp.Term() > this.Term()){
                this.updateCurrentTerm(resp.Term(), "");
                 return;
             }

            // panic response if it's not successful.
            if (!resp.Success()) {
                return;
             }

            // if one peer successfully append a log from the leader term,
            // we add it to the synced list
            if (resp.append == true) {
                this.syncedPeer[resp.peer] = true;
             }

            // Increment the commit count to make sure we have a quorum before committing.
            if (this.syncedPeer.Count < this.QuorumSize()) {
                return;
             }

            // Determine the committed index that a majority has.
            List<int> indices = new List<int>();
            indices.Add(this.log.currentIndex());
            foreach (var peer in this.peers)
            {
                indices.Add(peer.Value.getPrevLogIndex());
            }

            //TODO:sort
            //sort.Sort(sort.Reverse(uint64Slice(indices)));

            // We can commit up to the index which the majority of the members have appended.
            int commitIndex= indices[this.QuorumSize() - 1];

            int committedIndex= this.log.commitIndex;
             
             if (commitIndex > committedIndex) {
                // leader needs to do a fsync before committing log entries
                this.log.sync();
                this.log.setCommitIndex(commitIndex);
                DebugTrace.DebugLine("commit index ", commitIndex);
             }
        }

        // processVoteReponse processes a vote request:
        // 1. if the vote is granted for the current term of the candidate, return true
        // 2. if the vote is denied due to smaller term, update the term of this server
        //    which will also cause the candidate to step-down, and return false.
        // 3. if the vote is for a smaller term, ignore it and return false.
        public bool processVoteResponse(RequestVoteResponse resp)
        {
            if (resp.VoteGranted && resp.Term == this.currentTerm) {
                return true;
            }

            if (resp.Term > this.currentTerm)
            {
                DebugTrace.DebugLine("server.candidate.vote.failed");
                this.updateCurrentTerm(resp.Term, "");
            }
            else
            {
                DebugTrace.DebugLine("server.candidate.vote: denied");
            }
            return true;
        }

        //--------------------------------------
        // Request Vote
        //--------------------------------------

        // Requests a vote from a server. A vote can be obtained if the vote's term is
        // at the server's current term and the server has not made a vote yet. A vote
        // can also be obtained if the term is greater than the server's current term.
        public RequestVoteResponse RequestVote(RequestVoteRequest req)
        {
            //   ret, _ := this.send(req)
            //resp, _ := ret.(*RequestVoteResponse)
            //return resp
            return null;
        }

        // Processes a "request vote" request.
        public RequestVoteResponse processRequestVoteRequest(RequestVoteRequest req)
        {
            // If the request is coming from an old term then reject it.
            if (req.Term < this.Term())
            {
                DebugTrace.DebugLine("server.rv.deny.vote: cause stale term");

                return new RequestVoteResponse(this.currentTerm, false);
            }

            // If the term of the request peer is larger than this node, update the term
            // If the term is equal and we've already voted for a different candidate then
            // don't vote for this candidate.
            if (req.Term > this.Term())
            {
                this.updateCurrentTerm(req.Term, "");
            }
            else if (this.votedFor != "" && this.votedFor != req.CandidateName)
            {
                DebugTrace.DebugLine("server.deny.vote: cause duplicate vote: ", req.CandidateName,
                    " already vote for ", this.votedFor);
                return new RequestVoteResponse(this.currentTerm, false);
            }

            // If the candidate's log is not at least as up-to-date as our last log then don't vote.
            int lastIndex, lastTerm;
            this.log.lastInfo(out lastIndex, out lastTerm);

            if (lastIndex > req.LastLogIndex || lastTerm > req.LastLogTerm)
            {

                DebugTrace.DebugLine("server.deny.vote: cause out of date log: ", req.CandidateName,
                        "Index :[", lastIndex, "]", " [", req.LastLogIndex, "]",
                        "Term :[", lastTerm, "]", " [", req.LastLogTerm, "]");
                return new RequestVoteResponse(this.currentTerm, false);
            }

            // If we made it this far then cast a vote and reset our election time out.
            DebugTrace.DebugLine("server.rv.vote: ", this.name, " votes for", req.CandidateName, "at term", req.Term);

            this.votedFor = req.CandidateName;

            return new RequestVoteResponse(this.currentTerm, true);
        }

        //--------------------------------------
        // Membership
        //--------------------------------------

        /// <summary>
        /// Adds a peer to the server.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="connectiongString"></param>
        public void AddPeer(string name, string connectiongString)
        {
            DebugTrace.DebugLine("server.peer.add: ", name, this.peers.Count);

            // Do not allow peers to be added twice.
            if (this.peers[name] != null)
            {
                return;
            }

            // Skip the Peer if it has the same name as the Server
            if (this.name != name)
            {
                Peer peer = new Peer(this, name, connectiongString, this.heartbeatInterval);

                if (this.State() == ServerState.Leader)
                {
                    peer.startHeartbeat();
                }

                this.peers[peer.Name] = peer;
                this.DispatchAddPeerEvent(new RaftEventArgs(name, null));
            }
            // Write the configuration to file.
            this.writeConf();
        }

        // Removes a peer from the server.
        public void RemovePeer(string name)
        {
            DebugTrace.DebugLine("server.peer.remove: ", name, this.peers.Count);

            // Skip the Peer if it has the same name as the Server
            if (name != this.Name())
            {
                // Return error if peer doesn't exist.
                Peer peer = this.peers[name];
                if (peer == null)
                {
                    Console.Error.WriteLine(string.Format("raft: Peer not found: %s", name));
                    return;
                }
                // Stop peer and remove it.
                if (this.State() == ServerState.Leader)
                {
                    peer.stopHeartbeat(true);
                }
                this.peers.Remove(name);
                this.DispatchRemovePeerEvent(new RaftEventArgs(name, null));
            }

            // Write the configuration to file.
            this.writeConf();
        }

        //--------------------------------------
        // Log compaction
        //--------------------------------------
        public void TakeSnapshot()
        {
            if (this.stateMachine == null)
            {
                throw new Exception("Snapshot: Cannot create snapshot. Missing state machine.");
            }

            // Shortcut without lock
            // Exit if the server is currently creating a snapshot.
            if (this.pendingSnapshot != null)
            {
                throw new Exception("Snapshot: Last snapshot is not finished.");
            }

            // TODO: acquire the lock and no more committed is allowed
            // This will be done after finishing refactoring heartbeat
            DebugTrace.DebugLine("take.snapshot");


            int lastIndex, lastTerm;
            this.log.commitInfo(out lastIndex, out lastTerm);

            // check if there is log has been committed since the last snapshot.
            if (lastIndex == this.log.startIndex)
            {
                return;
            }

            string path = this.SnapshotPath(lastIndex, lastTerm);
            // Attach snapshot to pending snapshot and save it to disk.
            this.pendingSnapshot = new Snapshot() { LastIndex = lastIndex, LastTerm = lastTerm, Peers = null, State = null, Path = path };


            byte[] state = this.stateMachine.Save();

            // Clone the list of peers.
            List<Peer> peers = new List<Peer>();
            foreach (var peeritem in this.peers)
            {
                peers.Add(peeritem.Value);
            }
            peers.Add(new Peer { Name = this.Name(), ConnectionString = this.connectionString });

            // Attach snapshot to pending snapshot and save it to disk.
            this.pendingSnapshot.Peers = peers;
            this.pendingSnapshot.State = state;
            this.saveSnapshot();

                // We keep some log entries after the snapshot.
                // We do not want to send the whole snapshot to the slightly slow machines
            if (lastIndex - this.log.startIndex > Constants. NumberOfLogEntriesAfterSnapshot) {
                int compactIndex = lastIndex - Constants.NumberOfLogEntriesAfterSnapshot;

                int compactTerm = this.log.getEntry(compactIndex).Term();
                this.log.compact(compactIndex, compactTerm);
            }
        }

        // Retrieves the log path for the server.
        public void saveSnapshot()
        {
            if (this.pendingSnapshot == null)
            {
                throw new Exception("pendingSnapshot.is.null");
            }
            // Write snapshot to disk.
            this.pendingSnapshot.save();
            // Swap the current and last snapshots.
            var tmp = this.snapshot;
            this.snapshot = this.pendingSnapshot;

            // Delete the previous snapshot if there is any change
            if (tmp != null && !(tmp.LastIndex == this.snapshot.LastIndex && tmp.LastTerm == this.snapshot.LastTerm))
            {
                tmp.remove();

            }
            this.pendingSnapshot = null;
        }

        // Retrieves the log path for the server.
        public string SnapshotPath(int lastIndex, int lastTerm)
        {
            return Path.Combine(this.path, "snapshot", string.Format("%d_%d.ss", lastTerm, lastIndex));
        }

        public SnapshotResponse RequestSnapshot(SnapshotRequest req)
        {
            //   ret, _ := this.send(req)
            //resp, _ := ret.(*SnapshotResponse)
            //return resp
            return null;
        }

        public SnapshotResponse processSnapshotRequest(SnapshotRequest req)
        {
            // If the follower’s log contains an entry at the snapshot’s last index with a term
            // that matches the snapshot’s last term, then the follower already has all the
            // information found in the snapshot and can reply false.
            var entry = this.log.getEntry(req.LastIndex);

            if( entry != null && entry.Term() == req.LastTerm)
               {
                return new SnapshotResponse(false);
               }

            // Update state.
            this.setState(ServerState.Snapshotting);

            return new SnapshotResponse(true);
        }

        public SnapshotRecoveryResponse SnapshotRecoveryRequest(SnapshotRecoveryRequest req)
        {
            //   ret, _ := this.send(req)
            //resp, _ := ret.(*SnapshotRecoveryResponse)
            //return resp
            return null;
        }

        public SnapshotRecoveryResponse processSnapshotRecoveryRequest(SnapshotRecoveryRequest req)
        {
            // Recover state sent from request.
            this.stateMachine.Recovery(req.State);

            // Recover the cluster configuration.
            this.peers = new Dictionary<string, Peer>();
            foreach (var peer in req.Peers)
            {
                this.AddPeer(peer.Name, peer.ConnectionString);
            }

            // Update log state.
            this.currentTerm = req.LastTerm;
            this.log.updateCommitIndex(req.LastIndex);

            // Create local snapshot.
            this.pendingSnapshot = new Snapshot() {
                LastIndex = req.LastIndex,
                LastTerm = req.LastTerm,
                Peers = req.Peers,
                State = req.State,
                Path = this.SnapshotPath(req.LastIndex, req.LastTerm)
            };
            this.saveSnapshot();

            // Clear the previous log entries.
            this.log.compact(req.LastIndex, req.LastTerm);

            return new SnapshotRecoveryResponse(req.LastTerm, true, req.LastIndex);
        }

        // Load a snapshot at restart
        public void LoadSnapshot()
        {
            // Open snapshot/ directory.
            var filenames=Directory.GetFiles(Path.Combine(this.path, "snapshot"));
            if (filenames.Length == 0) {
                DebugTrace.DebugLine("no.snapshot.to.load");
                return;
            }

            // Grab the latest snapshot.
            //sort.Strings(filenames);

            string snapshotPath= Path.Combine(this.path, "snapshot", filenames[filenames.Length - 1]);
            // Read snapshot data.
            using (FileStream file = File.Open(snapshotPath, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    // Check checksum.
                    byte[] crcBytes = new byte[8];
                    file.Read(crcBytes, 0, crcBytes.Length);
                    string checksum=System.Text.Encoding.Default.GetString(crcBytes);
                  
                    // Load remaining snapshot contents.
                    using (MemoryStream ms = new MemoryStream())
                    {
                        file.Read(ms.GetBuffer(), 0, (int)(file.Length - file.Position));
                        ms.Flush();
                        ms.Seek(0, SeekOrigin.Begin);
                        // Generate checksum.
                        Crc32 crc = new Crc32();
                        string byteChecksum = crc.CheckSum(ms);
                        if (!checksum.Equals(byteChecksum) )
                        {
                            DebugTrace.DebugLine(checksum, " ", byteChecksum);
                            throw new Exception("bad snapshot file");
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("checksum.err: bad.snapshot.file");
                }
            }

            try
            {
                // Recover snapshot into state machine.
                this.stateMachine.Recovery(this.snapshot.State);
            }
            catch (Exception err)
            {
                DebugTrace.DebugLine("recovery.snapshot.error: ", err);
            }

            // Recover cluster configuration.
            foreach (var peer in this.snapshot.Peers)
            {
                this.AddPeer(peer.Name, peer.ConnectionString);
            }
           
            // Update log state.
            this.log.startTerm = this.snapshot.LastTerm;
            this.log.startIndex = this.snapshot.LastIndex;
            this.log.updateCommitIndex(this.snapshot.LastIndex);
        }
        
        //--------------------------------------
        // Config File
        //--------------------------------------

        // Flushes commit index to the disk.
        // So when the raft server restarts, it will commit upto the flushed commitIndex.
        public void FlushCommitIndex()
        {
            DebugTrace.DebugLine("server.conf.update");
            // Write the configuration to file.
            this.writeConf();
        }

        public void writeConf()
        {
            List<Peer> peers = new List<Peer>();
            foreach (var peerItem in this.peers)
            {
                peers.Add(peerItem.Value.Clone());
            }

            Config r = new Config()
            {
                CommitIndex = this.log.commitIndex,
                Peers = peers
            };

            string confPath = Path.Combine(this.path, "conf");
            string tmpConfPath = Path.Combine(this.path, "conf.tmp");


            using (FileStream file = new FileStream(tmpConfPath, FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Config));
                ser.WriteObject(file,r);
            }
            File.Replace(tmpConfPath, confPath, confPath+".bak");
        }

        // Read the configuration for the server.
        public void readConf()
        {
            string confPath = Path.Combine(this.path, "conf");
            DebugTrace.DebugLine("readConf.open ", confPath);
            if (File.Exists(confPath))
            {
                using (FileStream file = File.Open(confPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    var conf = new Config();
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Config));
                    conf = (Config)ser.ReadObject(file);

                    if (conf != null)
                    {
                        this.log.updateCommitIndex(conf.CommitIndex);
                    }
                }
            }
        }

        //--------------------------------------
        // Debugging
        //--------------------------------------

        public void DebugLine(params object[] objs)
        {
            if (DebugTrace.LogLevel > DebugTrace.DEBUG)
            {
                DebugTrace.Debug(string.Format("[{0} Term:{1}]", this.name, this.Term()));
                DebugTrace.Debug(objs);
            }
        }

        public void TraceLine(params object[] objs)
        {
            if (DebugTrace.LogLevel > DebugTrace.TRACE)
            {
                DebugTrace.Trace(string.Format("[{0} Term:{1}]", this.name, this.Term()));
                DebugTrace.Trace(objs);
            }
        }

    }
}
