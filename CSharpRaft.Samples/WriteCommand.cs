using CSharpRaft.Command;
using System;
using System.IO;
using System.Text;

namespace CSharpRaft.Samples
{
    class WriteCommand : ICommand
    {
        public string Key;
        public string Value;

        public WriteCommand()
        {

        }

        public WriteCommand(string key, string value)
        {
            this.Key = key;
            this.Value = value;
        }

        // The name of the command in the log.
        public string CommandName
        {
            get
            {
                return "write";
            }
        }

        // Writes a value to a key.
        public object Apply(IContext context)
        {
            KeyValueDB db = context.Server.Context as KeyValueDB;

            db.Put(this.Key, this.Value);

            return UTF8Encoding.UTF8.GetBytes("join");
        }

        public bool Decode(Stream reader)
        {
            return false;
        }

        public bool Encode(Stream writer)
        {
            return false;
        }
    }
}
