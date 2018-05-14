namespace CSharpRaft
{
    /// <summary>
    /// State of Server
    /// </summary>
    public enum ServerState
    {
        Stopped,

        Initialized,

        Follower,

        Candidate,

        Leader,

        Snapshotting
    }
}
