using ProtoBuf;
using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace CSharpRaft
{
    // An internal event to be processed by the server's event loop.
    public class LogEvent
    {
        internal object target;
        internal object returnValue;
        internal Exception c;
    }

    // A log entry stores a single item in the log.
    public class LogEntry
    {
        internal protobuf.LogEntry pb;

        public int Position;  // position in the log file

        internal Log log;

        internal LogEvent ev;

        // Creates a new log entry associated with a log.
        public LogEntry(Log log, LogEvent ev,  int index, int term, Command command)
        {
            MemoryStream ms = new MemoryStream();
            string commandName;
            if (command != null)
            {
                commandName = command.CommandName();
                if (command is CommandEncoder)
                {
                    (command as CommandEncoder).Encode(ms);
                }
                else
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(command.GetType());
                    ser.WriteObject(ms, command);
                }

                this.pb = new protobuf.LogEntry()
                {
                    Index = (uint)index,
                    Term = (uint)term,
                    CommandName = commandName,
                    Command= ms.ToArray()
                };
                this.log = log;
                this.ev = ev;
            }
        }

        public int Index()
        {
            return (int)this.pb.Index;
        }

        public int Term()
        {
            return (int)this.pb.Term;
        }

        public string CommandName()
        {
            return this.pb.CommandName;
        }

        public byte[] Command()
        {
            return this.pb.Command;
        }

        // Encodes the log entry to a buffer. Returns the number of bytes
        // written and any error that may have occurred.
        public void Encode(Stream writer)
        {
            Serializer.Serialize<protobuf.LogEntry>(writer, this.pb);
        }

        // Decodes the log entry from a buffer. Returns the number of bytes read and
        // any error that occurs.
        public void Decode(Stream reader)
        {
            this.pb = Serializer.Deserialize<protobuf.LogEntry>(reader);
        }

    }
}