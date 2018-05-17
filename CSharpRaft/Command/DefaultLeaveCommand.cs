using CSharpRaft.Serialize;
using System;
using System.IO;
using System.Text;

namespace CSharpRaft.Command
{
    /// <summary>
    /// Default Leave command
    /// </summary>
    public class DefaultLeaveCommand: ICommand
    {
        /// <summary>
        /// Node name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The name of the Leave command in the log
        /// </summary>
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

        public bool Encode(Stream stream)
        {
            try
            {
                SerializerFactory.GetSerilizer().Serialize<DefaultLeaveCommand>(stream, this);
                return true;
            }
            catch (Exception err)
            {
                DebugTrace.DebugLine("command.DefaultJoinCommand.Encode" + err);
                return false;
            }
        }

        public bool Decode(Stream stream)
        {
            DefaultLeaveCommand cmd = SerializerFactory.GetSerilizer().Deserialize<DefaultLeaveCommand>(stream);
            if (cmd != null)
            {
                this.Name = cmd.Name;
                return true;
            }
            return false;
        }
    }

   
}