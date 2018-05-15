using System.IO;

namespace CSharpRaft.Command
{
    /// <summary>
    /// NOP command
    /// </summary>
    public class NOPCommand : ICommand
    {
        // The name of the NOP command in the log
        public string CommandName
        {
            get
            {
                return "raft:nop";
            }
        }

        public object Apply(IContext context)
        {
            return null;
        }

        public bool Encode(Stream writer)
        {
            return false;
        }

        public bool Decode(Stream reader)
        {
            return false;
        }
    }
}
