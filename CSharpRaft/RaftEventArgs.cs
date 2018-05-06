using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpRaft
{
    public delegate void RaftEventHandler(object sender, RaftEventArgs e);

    public class RaftEventArgs: EventArgs
    {
        public RaftEventArgs() {

        }

        public RaftEventArgs(object value, object prevValue)
        {
            this.Value = value;
            this.PrevValue = prevValue;
        }

        public object Value;
        public object PrevValue;
    }
}
