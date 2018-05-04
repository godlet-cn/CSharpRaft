using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpRaft
{
    public class Commands
    {
        Dictionary<string, Command> commandTypes;

        public Commands()
        {
            commandTypes = new Dictionary<string, Command>();
        }

        // Creates a new instance of a command by name.
        public Command newCommand(string name, byte[] data)
        {
            // Find the registered command.
            Command command = commandTypes[name];
            if (command == null)
            {
                throw new System.Exception("raft.Command: Unregistered command type:" + name);

            }
            Command copy = this.Clone(command);

            return copy;
        }

        private Command Clone(Command command)
        {
            return command;
        }

        // Registers a command by storing a reference to an instance of it.
        public void RegisterCommand(Command command)
        {
            if (command == null)
            {
                throw new System.Exception("raft: Cannot register nil");
            }
            else if (commandTypes[command.CommandName()] != null)
            {
                throw new System.Exception("raft: Duplicate registration: " + command.CommandName());
            }
            commandTypes[command.CommandName()] = command;
        }
    }


}
