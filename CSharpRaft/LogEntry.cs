using ProtoBuf;
using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace CSharpRaft
{
    // An internal event to be processed by the server's event loop.
    public class LogEvent
    {
        internal object target;

        internal object returnValue;

        internal Exception c;
    }

    /// <summary>
    /// A log entry stores a single item in the log.
    /// </summary>
    public class LogEntry
    {
        internal protobuf.LogEntry pb;

        // position in the log file
        internal long Position;

        internal Log log;

        internal LogEvent ev;

        public LogEntry()
        {

        }

        // Creates a new log entry associated with a log.
        public LogEntry(Log log, LogEvent ev, int index, int term, Command command)
        {
            MemoryStream ms = new MemoryStream();
            string commandName;
            if (command != null)
            {
                commandName = command.CommandName;
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
                    Command = ms.ToArray()
                };
                this.log = log;
                this.ev = ev;
            }
        }

        public int Index
        {
            get
            {
                return (int)this.pb.Index;
            }
        }

        public int Term
        {
            get
            {
                return (int)this.pb.Term;
            }
        }

        public string CommandName
        {
            get
            {
                return this.pb.CommandName;
            }
        }

        public byte[] Command
        {
            get
            {
                return this.pb.Command;
            }
        }

        // Encodes the log entry to a buffer. Returns the number of bytes
        // written and any error that may have occurred.
        public int Encode(Stream writer)
        {
            int size = 0;
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize<protobuf.LogEntry>(ms, this.pb);

                byte[] data = ms.ToArray();
                byte[] len = BitConverter.GetBytes(data.Length);

                writer.Write(len, 0, len.Length);
                writer.Write(data, 0, data.Length);

                size = len.Length + data.Length;
            }
            return size;
        }

        // Decodes the log entry from a buffer. Returns the number of bytes read and
        // any error that occurs.
        public void Decode(Stream reader)
        {
            byte[] lenData = new byte[8];
            reader.Read(lenData, 0, 8);
            int len = BitConverter.ToInt32(lenData, 0);

            byte[] data = new byte[len];
            reader.Read(data, 0, len);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(data, 0, data.Length);
                ms.Flush();
                ms.Seek(0, SeekOrigin.Begin);

                this.pb = Serializer.Deserialize<protobuf.LogEntry>(ms);
            }
        }

    }
}