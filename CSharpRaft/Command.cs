using System.IO;
using System.Text;

namespace CSharpRaft
{
    /// <summary>
    /// Command represents an action to be taken on the replicated state machine.
    /// </summary>
    public interface Command
    {
        /// <summary>
        /// The name of Command
        /// </summary>
        string CommandName { get; }

        /// <summary>
        /// Apply this command to the server.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        object Apply(IContext context);
    }

    public interface CommandEncoder
    {
        bool Encode(Stream writer);

        bool Decode(Stream reader);
    }

    /// <summary>
    /// Default Join command
    /// </summary>
    public class DefaultJoinCommand: Command
    {
        /// <summary>
        /// Node name
        /// </summary>
        public string Name { get; set; }

        public string ConnectionString { get; set; }

        // The name of the Join command in the log
        public string CommandName
        {
            get
            {
                return "raft:join";
            }
        }

        public object Apply(IContext context)
        {
            context.Server.AddPeer(this.Name, this.ConnectionString);
            return UTF8Encoding.UTF8.GetBytes("join");
        }
    }

    /// <summary>
    /// Default Leave command
    /// </summary>
    public class DefaultLeaveCommand: Command
    {
        /// <summary>
        /// Node name
        /// </summary>
        public string Name { get; set; }

        // The name of the Leave command in the log
        public string CommandName
        {
            get
            {
                return "raft:leave";
            }
        }

        public object Apply(IContext context)
        {
            context.Server.RemovePeer(this.Name);
            return UTF8Encoding.UTF8.GetBytes("leave");
        }
    }

    /// <summary>
    /// NOP command
    /// </summary>
    public class NOPCommand: Command, CommandEncoder
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

        public bool Encode(Stream writer) {
            return false;
        }

        public bool Decode(Stream reader)
        {
            return false;
        }
    }
}