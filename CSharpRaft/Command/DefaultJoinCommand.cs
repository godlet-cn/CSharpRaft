using CSharpRaft.Serialize;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace CSharpRaft.Command
{
    /// <summary>
    /// Default Join command
    /// </summary>
    public class DefaultJoinCommand : ICommand
    {
        /// <summary>
        /// Node name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Connection information
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The name of the Join command in the log
        /// </summary>
        [JsonIgnore]
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

        public bool Encode(Stream stream)
        {
            try
            {
                SerializerFactory.GetSerilizer().Serialize<DefaultJoinCommand>(stream, this);
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
            DefaultJoinCommand cmd = SerializerFactory.GetSerilizer().Deserialize<DefaultJoinCommand>(stream);
            if (cmd != null)
            {
                this.Name = cmd.Name;
                this.ConnectionString = cmd.ConnectionString;
                return true;
            }
            return false;
        }
    }
}
