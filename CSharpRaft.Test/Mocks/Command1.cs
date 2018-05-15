using System;
using System.IO;
using CSharpRaft;
using CSharpRaft.Command;

namespace CSharpRaft.Test.Mocks
{
    class Command1 : ICommand
    {
        public string Val { get; set; }

        public string CommandName
        {
            get {
                return "Command1";
            }
        }

        public object Apply(IContext context) {
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
