using CSharpRaft.Transport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CSharpRaft
{
    public class Server : IServer
    {
        internal Log log;

        private bool isStopped;

        private Dictionary<string, Peer> peers;

        private Dictionary<string, bool> syncedPeer;

        private int electionTimeout;

        private int heartbeatInterval;

        private Snapshot snapshot;

        // PendingSnapshot is an unfinished snapshot.
        // After the pendingSnapshot is saved to disk,
        // it will be set to snapshot and also will be set to null.
        private Snapshot pendingSnapshot;

        internal int maxLogEntriesPerRequest;

        private string connectionString;

        private readonly object mutex = new object();

        #region Constructor

        /// <summary>
        /// Creates a new server with a log at the given path.   
        /// </summary>
        /// <param name="name">The name of server</param>
        /// <param name="path">The root path of server</param>
        /// <param name="transporter">transporter can not be null</param>
        /// <param name="stateMachine">stateMachine can be null if snapshotting and log compaction is to be disabled.</param>
        /// <param name="context">context can be anything (including null) and is not used by the raft package except returned by Server.Context().</param>
        /// <param name="connectionString">connectionString can be anything.</param>
        public Server(string name, string path, ITransporter transporter, StateMachine stateMachine, object context, string connectionString)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("raft.Server: Name cannot be blank");
            }
            if (transporter == null)
            {
                throw new ArgumentNullException("raft: Transporter required");
            }

            this.name = name;
            this.path = path;
            this.transporter = transporter;
            this.stateMachine = stateMachine;
            this.context = context;
            this.connectionString = connectionString;

            this.state = ServerState.Stopped;
            this.peers = new Dictionary<string, Peer>();
            this.log = new Log();

            this.electionTimeout = Constants.DefaultElectionTimeout;
            this.heartbeatInterval = Constants.DefaultHeartbeatInterval;
            this.maxLogEntriesPerRequest = Constants.MaxLogEntriesPerRequest;

            this.log.ApplyFunc = (LogEntry e, Command c) =>
            {
                if (Commited != null)
                {
                    Commited(this, new RaftEventArgs(e, null));
                }

                c.Apply(new Context(this, this.currentTerm, this.log.internalCurrentIndex, this.log.CommitIndex));
                return this;
            };
        }

        #endregion

        #region event

        public event RaftEventHandler StateChanged;
        public event RaftEventHandler LeaderChanged;
        public event RaftEventHandler TermChanged;
        public event RaftEventHandler Commited;
        public event RaftEventHandler PeerAdded;
        public event RaftEventHandler PeerRemoved;
        public event RaftEventHandler HeartbeatIntervalReached;
        public event RaftEventHandler ElectionTimeoutThresholdReached;
        public event RaftEventHandler HeartbeatReached;

        internal void DispatchStateChangeEvent(RaftEventArgs args)
        {
            StateChanged?.Invoke(this, args);
        }

        internal void DispatchLeaderChangeEvent(RaftEventArgs args)
        {
            LeaderChanged?.Invoke(this, args);
        }
        internal void DispatchTermChangeEvent(RaftEventArgs args)
        {
            TermChanged?.Invoke(this, args);
        }
        internal void DispatchCommiteEvent(RaftEventArgs args)
        {
            Commited?.Invoke(this, args);
        }
        internal void DispatchAddPeerEvent(RaftEventArgs args)
        {
            PeerAdded?.Invoke(this, args);
        }
        internal void DispatchRemovePeerEvent(RaftEventArgs args)
        {
            PeerRemoved?.Invoke(this, args);
        }
        internal void DispatchHeartbeatIntervalEvent(RaftEventArgs args)
        {
            HeartbeatIntervalReached?.Invoke(this, args);
        }
        internal void DispatchElectionTimeoutThresholdEvent(RaftEventArgs args)
        {
            ElectionTimeoutThresholdReached?.Invoke(this, args);
        }
        internal void DispatchHeartbeatEvent(RaftEventArgs args)
        {
            HeartbeatReached?.Invoke(this, args);
        }

        #endregion

        #region Properties

        private string name;
        /// <summary>
        /// The name of the server.
        /// </summary>
        public string Name
        {
            get
            {
                return this.name;
            }
        }

        private string path;
        /// <summary>
        /// The storage path for the server.
        /// </summary>
        public string Path
        {
            get
            {
                return this.path;
            }
        }

        private string leader;
        /// <summary>
        /// The name of the current leader.
        /// </summary>
        public string Leader
        {
            get
            {
                return this.leader;
            }
        }

        private ITransporter transporter;
        /// <summary>
        /// The object that transports requests.
        /// </summary>
        public ITransporter Transporter
        {
            get
            {
                return this.transporter;
            }
            set
            {
                lock (mutex)
                {
                    this.transporter = value;
                }
            }
        }

        private object context;
        /// <summary>
        /// The context passed into the constructor.
        /// </summary>
        public object Context
        {
            get
            {
                return this.context;
            }
        }

        private StateMachine stateMachine;
        /// <summary>
        /// The state machine passed into the constructor.
        /// </summary>
        public StateMachine StateMachine
        {
            get
            {
                return this.stateMachine;
            }
        }

        /// <summary>
        /// The log path for the server.
        /// </summary>
        public string LogPath
        {
            get
            {
                return System.IO.Path.Combine(this.path, "log");
            }
        }

        private ServerState state;
        /// <summary>
        /// The current state of the server.
        /// </summary>
        public ServerState State
        {
            get
            {
                lock (mutex)
                {
                    return this.state;
                }
            }
        }

        private int currentTerm;
        /// <summary>
        /// The current term of the server.
        /// </summary>
        public int Term
        {
            get
            {
                lock (mutex)
                {
                    return this.currentTerm;
                }
            }
        }

        /// <summary>
        /// The current commit index of the server.
        /// </summary>
        public int CommitIndex
        {
            get
            {
                return this.log.CommitIndex;
            }
        }

        private string votedFor;
        /// <summary>
        /// The name of the candidate this server voted for in this term.
        /// </summary>
        public string VotedFor
        {
            get
            {
                return this.votedFor;
            }
        }

        /// <summary>
        /// Whether the server's log has no entries.
        /// </summary>
        public bool IsLogEmpty
        {
            get
            {
                return this.log.isEmpty;
            }
        }

        // A list of all the log entries. This should only be used for debugging purposes.
        public List<LogEntry> LogEntries
        {
            get
            {
                return this.log.entries;
            }
        }

        // A reference to the command name of the last entry.
        public string LastCommandName
        {
            get
            {
                return this.log.lastCommandName;
            }
        }

        // Check if the server is promotable
        public bool promotable()
        {
            return this.log.currentIndex > 0;
        }

        //--------------------------------------
        // Membership
        //--------------------------------------

        /// <summary>
        /// The number of member servers in the consensus.
        /// </summary>
        public int MemberCount
        {
            get
            {
                lock (mutex)
                {
                    return this.peers.Count + 1;
                }
            }
        }

        /// <summary>
        /// The number of servers required to make a quorum.
        /// </summary>
        public int QuorumSize
        {
            get
            {
                return (this.MemberCount / 2) + 1;
            }
        }

        /// <summary>
        ///  The election timeout.
        /// </summary>
        public int ElectionTimeout
        {
            get
            {
                return this.electionTimeout;
            }
            set
            {
                lock (mutex)
                {
                    this.electionTimeout = value;
                }
            }
        }

        /// <summary>
        /// The heartbeat timeout.
        /// </summary>
        public int HeartbeatInterval
        {
            get
            {
                return this.heartbeatInterval;
            }
            set
            {
                lock (mutex)
                {
                    this.heartbeatInterval = value;

                    foreach (var peer in this.peers)
                    {
                        peer.Value.setHeartbeatInterval(value);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the server is currently running.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                lock (mutex)
                {
                    return (this.state != ServerState.Stopped && this.state != ServerState.Initialized);
                }
            }
        }

        #endregion

        // Sets the state of the server.
        private void setState(ServerState state)
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
                    this.leader = this.Name;

                    this.syncedPeer = new Dictionary<string, bool>();
                }

                // Dispatch state and leader change events.
                this.DispatchStateChangeEvent(new RaftEventArgs(this.state, prevState));

                if (prevLeader != this.leader)
                {
                    this.DispatchLeaderChangeEvent(new RaftEventArgs(this.leader, prevLeader));
                }
            }
        }

        // Init initializes the raft server.
        // If there is no previous log file under the given path, Init() will create an empty log file.
        // Otherwise, Init() will load in the log entries from the log file.
        public void Init()
        {
            if (this.IsRunning)
            {
                Console.Error.WriteLine(string.Format("raft.Server: Server already running[{0}]", this.state));
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

            string snapshotDir = System.IO.Path.Combine(this.path, "snapshot");
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
                this.debugLine("raft: Snapshot directory doesn't exist");
                Console.Error.WriteLine(string.Format("raft: Initialization error: {0}", ex));
                return;
            }

            try
            {
                ConfigHelper.ReadConfig(this);
            }
            catch (Exception err)
            {
                this.debugLine("raft: Conf file error: ", err);
                Console.Error.WriteLine(string.Format("raft: Initialization error: {0}", err));
                return;
            }

            try
            {
                // Initialize the log and load it up.
                this.log.open(this.LogPath);
            }
            catch (Exception err)
            {
                this.debugLine("raft: Log error: ", err);
                Console.Error.WriteLine(string.Format("raft: Initialization error: {0}", err));
                return;
            }

            // Update the term to the last term in the log.
            int index, curTerm;
            this.log.getLastInfo(out index, out curTerm);
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
            if (this.IsRunning)
            {
                throw new Exception(string.Format("raft.Server: Server already running[{0}]", this.state));
            }
            this.Init();

            this.setState(ServerState.Follower);

            // If no log entries exist then
            // 1. wait for AEs(AppendEntities) from another node
            // 2. wait for self-join command
            // to set itself promotable
            if (!this.promotable())
            {
                this.debugLine("start as a new raft server");

                // If log entries exist then allow promotion to candidate
                // if no AEs received.
            }
            else
            {
                this.debugLine("start from previous saved state");
            }

            this.debugLine(this.State);

            Task.Factory.StartNew(() =>
            {
                this.loop();
            });
        }

        // Shuts down the server.
        public void Stop()
        {
            if (this.State == ServerState.Stopped)
            {
                return;
            }

            this.isStopped = true;

            this.log.close();

            this.setState(ServerState.Stopped);
        }

        //--------------------------------------
        // Term
        //--------------------------------------

        // updates the current term for the server. This is only used when a larger
        // external term is found.
        private void updateCurrentTerm(int term, string leaderName)
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
            this.DispatchTermChangeEvent(new RaftEventArgs(this.currentTerm, prevTerm));

            if (prevLeader != this.leader)
            {
                this.DispatchLeaderChangeEvent(new RaftEventArgs(this.leader, prevLeader));
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
        private void loop()
        {
            ServerState state = this.State;

            while (state != ServerState.Stopped)
            {
                this.debugLine("server.loop.run ", state);
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
                state = this.State;
            }

            this.debugLine("server.loop.end");
        }

        // The event loop that is run when the server is in a Follower state.
        // Responds to RPCs from candidates and leaders.
        // Converts to candidate if election timeout elapses without either:
        //   1.Receiving valid AppendEntries RPC, or
        //   2.Granting vote to candidate
        private void followerLoop()
        {
            DateTime since = DateTime.Now;

            int electionTimeout = this.ElectionTimeout;

            Random rand = new Random();

            int timeoutChan = rand.Next(this.ElectionTimeout, this.ElectionTimeout * 2);

            while (this.State == ServerState.Follower)
            {
                if (this.isStopped)
                {
                    this.setState(ServerState.Stopped);
                    return;
                }

                //          var err error
                bool update = false;


                //      select {

                //case e:= < -this.c:
                //	switch req := e.target.(type) {

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

                if (update)
                {
                    since = DateTime.Now;

                    //timeoutChan = afterBetween(this.ElectionTimeout(), this.ElectionTimeout() * 2);
                }
            }
        }

        // The event loop that is run when the server is in a Candidate state.
        private void candidateLoop()
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
            this.log.getLastInfo(out lastLogIndex, out lastLogTerm);


            //  doVote:= true

            //  votesGranted:= 0

            //  var timeoutChan<-chan time.Time
            // var respChan chan *RequestVoteResponse


            while (this.State == ServerState.Candidate)
            {
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
                //              this.debugLine("server.candidate.recv.enough.votes")

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
                //                  this.debugLine("server.candidate.vote.granted: ", votesGranted)

                //              votesGranted++

                //          }

                //case e:= < -this.c:
                //	var err error

                //          switch req := e.target.(type) {

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
            }
        }

        // The event loop that is run when the server is in a Leader state.
        private void leaderLoop()
        {
            int logIndex, logTerm;
            this.log.getLastInfo(out logIndex, out logTerm);

            // Update the peers prevLogIndex to leader's lastLogIndex and start heartbeat.
            this.debugLine("leaderLoop.set.PrevIndex to ", logIndex);

            foreach (var peerItem in this.peers)
            {
                peerItem.Value.PrevLogIndex = logIndex;
                peerItem.Value.startHeartbeat();
            }

            // Commit a NOP after the server becomes leader. From the Raft paper:
            // "Upon election: send initial empty AppendEntries RPCs (heartbeat) to
            // each server; repeat during idle periods to prevent election timeouts
            // (§5.2)". The heartbeats started above do the "idle" period work.

            Task.Factory.StartNew(new Action(() =>
            {
                this.Do(new NOPCommand { });
            }));

            // Begin to collect response from followers
            while (this.State == ServerState.Leader)
            {
                if (this.isStopped)
                {
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

        private void snapshotLoop()
        {
            while (this.State == ServerState.Snapshotting)
            {
                if (this.isStopped)
                {
                    this.setState(ServerState.Stopped);
                    return;
                }

                //      var err error
                //      select {
                //case e:= < -this.c:
                //	switch req := e.target.(type) {
                //	case *AppendEntriesRequest:
                //		e.returnValue, _ = this.processAppendEntriesRequest(req)
                //	case *RequestVoteRequest:
                //		e.returnValue, _ = this.processRequestVoteRequest(req)
                //	case *SnapshotRecoveryRequest:
                //		e.returnValue = this.processSnapshotRecoveryRequest(req)
                //  }
                //          // Callback to event.
                //          e.c < -err
                //      }
            }
        }

        //--------------------------------------
        // Commands
        //--------------------------------------

        // Attempts to execute a command and replicate it. The function will return
        // when the command has been successfully committed or an error has occurred.
        public object Do(Command command)
        {
            switch (this.State)
            {
                case ServerState.Follower:
                    if (command is DefaultJoinCommand)
                    {
                        //If no log entries exist and a self-join command is issued
                        //then immediately become leader and commit entry.
                        if (this.log.currentIndex == 0 && (command as DefaultJoinCommand).Name == this.Name)
                        {
                            this.debugLine("self join and promote to leader");
                            this.setState(ServerState.Leader);

                            return this.processCommand(command);
                        }
                        else
                        {
                            throw Constants.NotLeaderError;
                        }
                    }
                    break;
                case ServerState.Leader:
                    return this.processCommand(command);
                case ServerState.Candidate:
                case ServerState.Snapshotting:
                    throw Constants.NotLeaderError;
            }
            return null;
        }

        // Processes a command.
        private bool processCommand(Command command)
        {
            this.debugLine("server.command.process:"+command.CommandName);

            try
            {
                // Create an entry for the command in the log.
                var entry = this.log.createEntry(this.currentTerm, command);
                this.log.appendEntry(entry);

                this.syncedPeer[this.Name] = true;

                if (this.peers.Count == 0)
                {
                    int commitIndex = this.log.currentIndex;

                    this.log.setCommitIndex(commitIndex);

                    this.debugLine("commit index ", commitIndex);
                }
                return true;
            }
            catch (Exception err)
            {
                this.debugLine("server.command.log.entry.error:", err);
                return false;
            }
        }

        //--------------------------------------
        // Append Entries
        //--------------------------------------

        // Appends zero or more log entry from the leader to this server.
        public AppendEntriesResponse AppendEntries(AppendEntriesRequest req)
        {
            return this.processAppendEntriesRequest(req);
        }

        // Processes the "append entries" request.
        private AppendEntriesResponse processAppendEntriesRequest(AppendEntriesRequest req)
        {
            this.traceLine("server.ae.process");


            if (req.Term < this.currentTerm)
            {
                this.debugLine("server.ae.error: stale term");

                return new AppendEntriesResponse(this.currentTerm, false, this.log.currentIndex, this.log.CommitIndex);
            }

            if (req.Term == this.currentTerm)
            {
                if (this.State == ServerState.Leader)
                {
                    throw new Exception(string.Format("leader.elected.at.same.term.{0}\n", this.currentTerm));
                }
                // step-down to follower when it is a candidate
                if (this.state == ServerState.Candidate)
                {
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
                this.debugLine("server.ae.truncate.error: ", err);
                return new AppendEntriesResponse(this.currentTerm, false, this.log.currentIndex, this.log.CommitIndex);
            }

            // Append entries to the log.
            try
            {
                this.log.appendEntries(req.Entries);
            }
            catch (Exception err)
            {
                this.debugLine("server.ae.append.error: ", err);
                return new AppendEntriesResponse(this.currentTerm, false, this.log.currentIndex, this.log.CommitIndex);
            }

            try
            {
                // Commit up to the commit index.
                this.log.setCommitIndex(req.CommitIndex);
            }
            catch (Exception err)
            {
                this.debugLine("server.ae.commit.error: ", err);

                return new AppendEntriesResponse(this.currentTerm, false, this.log.currentIndex, this.log.CommitIndex);
            }

            // once the server appended and committed all the log entries from the leader
            return new AppendEntriesResponse(this.currentTerm, true, this.log.currentIndex, this.log.CommitIndex);
        }

        // Processes the "append entries" response from the peer. This is only
        // processed when the server is a leader. Responses received during other
        // states are dropped.
        private void processAppendEntriesResponse(AppendEntriesResponse resp)
        {
            // If we find a higher term then change to a follower and exit.
            if (resp.Term > this.Term)
            {
                this.updateCurrentTerm(resp.Term, "");
                return;
            }

            // panic response if it's not successful.
            if (!resp.Success)
            {
                return;
            }

            // if one peer successfully append a log from the leader term,
            // we add it to the synced list
            if (resp.append == true)
            {
                this.syncedPeer[resp.peer] = true;
            }

            // Increment the commit count to make sure we have a quorum before committing.
            if (this.syncedPeer.Count < this.QuorumSize)
            {
                return;
            }

            // Determine the committed index that a majority has.
            List<int> indices = new List<int>();
            indices.Add(this.log.currentIndex);
            foreach (var peer in this.peers)
            {
                indices.Add(peer.Value.PrevLogIndex);
            }

            //TODO:sort
            //sort.Sort(sort.Reverse(uint64Slice(indices)));

            // We can commit up to the index which the majority of the members have appended.
            int commitIndex = indices[this.QuorumSize - 1];

            int committedIndex = this.log.CommitIndex;

            if (commitIndex > committedIndex)
            {
                // leader needs to do a fsync before committing log entries
                this.log.sync();
                this.log.setCommitIndex(commitIndex);
                this.debugLine("commit index ", commitIndex);
            }
        }

        // processVoteReponse processes a vote request:
        // 1. if the vote is granted for the current term of the candidate, return true
        // 2. if the vote is denied due to smaller term, update the term of this server
        //    which will also cause the candidate to step-down, and return false.
        // 3. if the vote is for a smaller term, ignore it and return false.
        private bool processVoteResponse(RequestVoteResponse resp)
        {
            if (resp.VoteGranted && resp.Term == this.currentTerm)
            {
                return true;
            }

            if (resp.Term > this.currentTerm)
            {
                this.debugLine("server.candidate.vote.failed");
                this.updateCurrentTerm(resp.Term, "");
            }
            else
            {
                this.debugLine("server.candidate.vote: denied");
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
            return this.processRequestVoteRequest(req);
        }

        // Processes a "request vote" request.
        private RequestVoteResponse processRequestVoteRequest(RequestVoteRequest req)
        {
            // If the request is coming from an old term then reject it.
            if (req.Term < this.Term)
            {
                this.debugLine("server.rv.deny.vote: cause stale term");

                return new RequestVoteResponse(this.currentTerm, false);
            }

            // If the term of the request peer is larger than this node, update the term
            // If the term is equal and we've already voted for a different candidate then
            // don't vote for this candidate.
            if (req.Term > this.Term)
            {
                this.updateCurrentTerm(req.Term, "");
            }
            else if (this.votedFor != "" && this.votedFor != req.CandidateName)
            {
                this.debugLine("server.deny.vote: cause duplicate vote: ", req.CandidateName,
                    " already vote for ", this.votedFor);
                return new RequestVoteResponse(this.currentTerm, false);
            }

            // If the candidate's log is not at least as up-to-date as our last log then don't vote.
            int lastIndex, lastTerm;
            this.log.getLastInfo(out lastIndex, out lastTerm);

            if (lastIndex > req.LastLogIndex || lastTerm > req.LastLogTerm)
            {

                this.debugLine("server.deny.vote: cause out of date log: ", req.CandidateName,
                        "Index :[", lastIndex, "]", " [", req.LastLogIndex, "]",
                        "Term :[", lastTerm, "]", " [", req.LastLogTerm, "]");
                return new RequestVoteResponse(this.currentTerm, false);
            }

            // If we made it this far then cast a vote and reset our election time out.
            this.debugLine("server.rv.vote: ", this.name, " votes for", req.CandidateName, "at term", req.Term);

            this.votedFor = req.CandidateName;

            return new RequestVoteResponse(this.currentTerm, true);
        }

        /// <summary>
        /// Retrieves a copy of the peers in this server
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, Peer> GetPeers()
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

        /// <summary>
        /// Adds a peer to the server.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="connectiongString"></param>
        public void AddPeer(string name, string connectiongString)
        {
            this.debugLine("server.peer.add: ", name, this.peers.Count);

            // Do not allow peers to be added twice.
            if (this.peers.ContainsKey(name))
            {
                return;
            }

            // Skip the Peer if it has the same name as the Server
            if (this.name != name)
            {
                Peer peer = new Peer(this, name, connectiongString, this.heartbeatInterval);

                if (this.State == ServerState.Leader)
                {
                    peer.startHeartbeat();
                }

                this.peers[peer.Name] = peer;

                this.DispatchAddPeerEvent(new RaftEventArgs(name, null));

                // Write the configuration to file.
                ConfigHelper.WriteConfig(this);
            }
        }

        // Removes a peer from the server.
        public void RemovePeer(string name)
        {
            this.debugLine("server.peer.remove: ", name, this.peers.Count);

            // Skip the Peer if it has the same name as the Server
            if (name != this.Name)
            {
                // Return error if peer doesn't exist.
                Peer peer = this.peers[name];
                if (peer == null)
                {
                    Console.Error.WriteLine(string.Format("raft: Peer not found: {0}", name));
                    return;
                }
                // Stop peer and remove it.
                if (this.State == ServerState.Leader)
                {
                    peer.stopHeartbeat(true);
                }
                this.peers.Remove(name);
                this.DispatchRemovePeerEvent(new RaftEventArgs(name, null));

                // Write the configuration to file.
                ConfigHelper.WriteConfig(this);
            }
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
            this.debugLine("take.snapshot");


            int lastIndex, lastTerm;
            this.log.getCommitInfo(out lastIndex, out lastTerm);

            // check if there is log has been committed since the last snapshot.
            if (lastIndex == this.log.startIndex)
            {
                return;
            }

            string path = this.GetSnapshotPath(lastIndex, lastTerm);
            // Attach snapshot to pending snapshot and save it to disk.
            this.pendingSnapshot = new Snapshot() { LastIndex = lastIndex, LastTerm = lastTerm, Peers = null, State = null, Path = path };


            byte[] state = this.stateMachine.Save();

            // Clone the list of peers.
            List<Peer> peers = new List<Peer>();
            foreach (var peeritem in this.peers)
            {
                peers.Add(peeritem.Value);
            }
            peers.Add(new Peer(this.Name, this.connectionString));

            // Attach snapshot to pending snapshot and save it to disk.
            this.pendingSnapshot.Peers = peers;
            this.pendingSnapshot.State = state;
            this.saveSnapshot();

            // We keep some log entries after the snapshot.
            // We do not want to send the whole snapshot to the slightly slow machines
            if (lastIndex - this.log.startIndex > Constants.NumberOfLogEntriesAfterSnapshot)
            {
                int compactIndex = lastIndex - Constants.NumberOfLogEntriesAfterSnapshot;

                int compactTerm = this.log.getEntry(compactIndex).Term;
                this.log.compact(compactIndex, compactTerm);
            }
        }

        // Retrieves the log path for the server.
        private void saveSnapshot()
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

        public Snapshot GetSnapshot()
        {
            return this.snapshot;
        }

        // Retrieves the log path for the server.
        public string GetSnapshotPath(int lastIndex, int lastTerm)
        {
            return System.IO.Path.Combine(this.path, "snapshot", string.Format("{0}_{0}.ss", lastTerm, lastIndex));
        }

        public SnapshotResponse RequestSnapshot(SnapshotRequest req)
        {
            return this.processSnapshotRequest(req);
        }

        private SnapshotResponse processSnapshotRequest(SnapshotRequest req)
        {
            // If the follower’s log contains an entry at the snapshot’s last index with a term
            // that matches the snapshot’s last term, then the follower already has all the
            // information found in the snapshot and can reply false.
            var entry = this.log.getEntry(req.LastIndex);

            if (entry != null && entry.Term == req.LastTerm)
            {
                return new SnapshotResponse(false);
            }

            // Update state.
            this.setState(ServerState.Snapshotting);

            return new SnapshotResponse(true);
        }

        public SnapshotRecoveryResponse SnapshotRecoveryRequest(SnapshotRecoveryRequest req)
        {
            return this.processSnapshotRecoveryRequest(req);
        }

        private SnapshotRecoveryResponse processSnapshotRecoveryRequest(SnapshotRecoveryRequest req)
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
            this.pendingSnapshot = new Snapshot()
            {
                LastIndex = req.LastIndex,
                LastTerm = req.LastTerm,
                Peers = req.Peers,
                State = req.State,
                Path = this.GetSnapshotPath(req.LastIndex, req.LastTerm)
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
            var filenames = Directory.GetFiles(System.IO.Path.Combine(this.path, "snapshot"));
            if (filenames.Length == 0)
            {
                this.debugLine("no.snapshot.to.load");
                return;
            }

            // Grab the latest snapshot.
            //sort.Strings(filenames);

            string snapshotPath = System.IO.Path.Combine(this.path, "snapshot", filenames[filenames.Length - 1]);
            // Read snapshot data.
            using (FileStream file = File.Open(snapshotPath, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    // Check checksum.
                    byte[] crcBytes = new byte[8];
                    file.Read(crcBytes, 0, crcBytes.Length);
                    string checksum = System.Text.Encoding.Default.GetString(crcBytes);

                    // Load remaining snapshot contents.
                    using (MemoryStream ms = new MemoryStream())
                    {
                        file.Read(ms.GetBuffer(), 0, (int)(file.Length - file.Position));
                        ms.Flush();
                        ms.Seek(0, SeekOrigin.Begin);
                        // Generate checksum.
                        Crc32 crc = new Crc32();
                        string byteChecksum = crc.CheckSum(ms);
                        if (!checksum.Equals(byteChecksum))
                        {
                            this.debugLine(checksum, " ", byteChecksum);
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
                this.debugLine("recovery.snapshot.error: ", err);
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

        // Flushes commit index to the disk.
        // So when the raft server restarts, it will commit upto the flushed commitIndex.
        public void FlushCommitIndex()
        {
            this.debugLine("server.conf.update");
            // Write the configuration to file.
            ConfigHelper.WriteConfig(this);
        }

        #region Debugging

        // Get the state of the server for debugging
        internal string getState()
        {
            lock (mutex)
            {
                return string.Format("Name: {0}, State: {1}, Term: {2}, CommitedIndex: {3} ", this.name, this.state, this.currentTerm, this.log.CommitIndex);
            }
        }

        internal void debugLine(params object[] objs)
        {
            if (DebugTrace.Level > LogLevel.DEBUG)
            {
                DebugTrace.Debug(string.Format("[{0} Term:{1}]: ", this.name, this.Term));
                DebugTrace.DebugLine(objs);
            }
        }

        internal void traceLine(params object[] objs)
        {
            if (DebugTrace.Level > LogLevel.TRACE)
            {
                DebugTrace.Trace(string.Format("[{0} Term:{1}]: ", this.name, this.Term));
                DebugTrace.DebugLine(objs);
            }
        }

        #endregion
    }
}
