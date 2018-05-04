using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpRaft
{
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
