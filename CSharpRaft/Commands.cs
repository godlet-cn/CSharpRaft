using System.Collections.Generic;

namespace CSharpRaft
{
    /// <summary>
    /// Commands store all commands
    /// </summary>
    public static class Commands
    {
        static Dictionary<string, Command> commandTypes;

        static Commands()
        {
            commandTypes = new Dictionary<string, Command>();
        }

        // Creates a new instance of a command by name.
        public static Command newCommand(string name, byte[] data)
        {
            // Find the registered command.
            Command command = commandTypes[name];
            if (command == null)
            {
                throw new System.Exception("raft.Command: Unregistered command type:" + name);

            }
            Command copy = Clone(command);

            return copy;
        }

        private static Command Clone(Command command)
        {
            return command;
        }

        // Registers a command by storing a reference to an instance of it.
        public static void RegisterCommand(Command command)
        {
            if (command == null)
            {
                throw new System.Exception("raft: Cannot register nil");
            }
            else if (commandTypes.ContainsKey(command.CommandName()) 
                &&commandTypes[command.CommandName()] != null)
            {
                throw new System.Exception("raft: Duplicate registration: " + command.CommandName());
            }
            commandTypes[command.CommandName()] = command;
        }
    }


}
