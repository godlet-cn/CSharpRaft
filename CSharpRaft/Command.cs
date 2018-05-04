using System;
using System.Collections.Generic;
using System.IO;

namespace CSharpRaft
{
    // Command represents an action to be taken on the replicated state machine.
    public interface Command
    {
        string CommandName();
    }

    // CommandApply represents the interface to apply a command to the server.
    public interface CommandApply
    {
        object Apply(Context context);
    }

    // deprecatedCommandApply represents the old interface to apply a command to the server.
    public interface deprecatedCommandApply
    {

        object Apply(Server server);
    }

    public interface CommandEncoder
    {
        bool Encode(BinaryWriter writer);
        bool Decode(BinaryReader reader);
    }


    // Join command interface
    public interface JoinCommand : Command
    {
        string NodeName();
    }

    // Join command
    public class DefaultJoinCommand: JoinCommand
    {
        string Name;
        string ConnectionString;
        
        // The name of the Join command in the log
        public string CommandName()
        {
            return "raft:join";
        }

        public object Apply(Server server)
        {
            server.AddPeer(this.Name, this.ConnectionString);
            return System.Text.UTF8Encoding.UTF8.GetBytes("join");
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
        string Name;


        // The name of the Leave command in the log
        public string CommandName()
        {
            return "raft:leave";
        }

        public object Apply(Server server)
        {
            server.RemovePeer(this.Name);
            return System.Text.UTF8Encoding.UTF8.GetBytes("leave");
        }

        public string NodeName()
        {
            return this.Name;
        }
    }

    // NOP command
    public class NOPCommand
    {

        // The name of the NOP command in the log
        public string CommandName()
        {
            return "raft:nop";
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