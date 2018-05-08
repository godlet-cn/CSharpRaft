using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSharpRaft
{
    /// <summary>
    /// Command represents an action to be taken on the replicated state machine.
    /// </summary>
    public interface Command
    {
        string CommandName { get; }
    }

    /// <summary>
    /// CommandApply represents the interface to apply a command to the server.
    /// </summary>
    public interface CommandApply
    {
        object Apply(IContext context);
    }

    /// <summary>
    /// deprecatedCommandApply represents the old interface to apply a command to the server.
    /// </summary>
    public interface deprecatedCommandApply
    {
        object Apply(Server server);
    }

    public interface CommandEncoder
    {
        bool Encode(Stream writer);
        bool Decode(Stream reader);
    }

    // Join command interface
    public interface JoinCommand : Command
    {
        string NodeName();
    }

    // Join command
    public class DefaultJoinCommand: JoinCommand
    {
        public string Name;
        public string ConnectionString;
        
        // The name of the Join command in the log
        public string CommandName
        {
            get
            {
                return "raft:join";
            }
        }

        public object Apply(Server server)
        {
            server.AddPeer(this.Name, this.ConnectionString);
            return UTF8Encoding.UTF8.GetBytes("join");
        }

        public string NodeName()
        {
            return this.Name;
        }
    }

    // Leave command interface
    public interface LeaveCommand : Command
    {
        string NodeName();
    }

    // Leave command
    public class DefaultLeaveCommand: LeaveCommand
    {
        public string Name;


        // The name of the Leave command in the log
        public string CommandName
        {
            get
            {
                return "raft:leave";
            }
        }

        public object Apply(Server server)
        {
            server.RemovePeer(this.Name);
            return UTF8Encoding.UTF8.GetBytes("leave");
        }

        public string NodeName()
        {
            return this.Name;
        }
    }

    /// <summary>
    /// NOP command
    /// </summary>
    public class NOPCommand: Command
    {
        // The name of the NOP command in the log
        public string CommandName
        {
            get
            {
                return "raft:nop";
            }
        }

        public object Apply(Server server)
        {
            return null;
        }

        public bool Encode(BinaryWriter writer) {
            return false;
        }

        public bool Decode(BinaryReader reader)
        {
            return false;
        }
    }
}