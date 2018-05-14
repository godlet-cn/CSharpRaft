using System.Collections.Generic;

namespace CSharpRaft
{
    /// <summary>
    /// The configuration of server
    /// </summary>
    public class Config
    {
        public int CommitIndex { get; set; }

        public List<Peer> Peers { get; set; }
    }
}
