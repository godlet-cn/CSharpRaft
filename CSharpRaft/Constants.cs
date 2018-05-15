using System;

namespace CSharpRaft
{
    public class Constants
    {
        public const int MaxLogEntriesPerRequest = 2000;

        public const int NumberOfLogEntriesAfterSnapshot = 200;
        
        //the interval that the leader will send(unit: millisecond)
        public const int DefaultHeartbeatInterval = 50;

        public const int DefaultElectionTimeout = 150;

        //specifies the threshold at which the server
        // will dispatch warning events that the heartbeat RTT is too close to the
        // election timeout.
        public const double ElectionTimeoutThresholdPercent = 0.8;


        public static Exception NotLeaderError = new Exception("raft.Server: Not current leader");

        public static Exception DuplicatePeerError = new Exception("raft.Server: Duplicate peer");

        public static Exception CommandTimeoutError = new Exception("raft: Command timeout");

        public static Exception StopError = new Exception("raft: Has been stopped");
    }
}
