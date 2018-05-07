using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
