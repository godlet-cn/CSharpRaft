using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpRaft
{
    public class Config
    {
        public int CommitIndex { get; set; }

        public List<Peer> Peers { get; set; }
    }
}
