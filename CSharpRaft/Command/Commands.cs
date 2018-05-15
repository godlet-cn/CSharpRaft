using System;
using System.Collections.Generic;
using System.IO;

namespace CSharpRaft.Command
{
    /// <summary>
    /// Commands store all commands
    /// </summary>
    public static class Commands
    {
        static Dictionary<string, ICommand> commandTypes;

        static Commands()
        {
            commandTypes = new Dictionary<string, ICommand>();

            Commands.RegisterCommand(new NOPCommand());
            Commands.RegisterCommand(new DefaultJoinCommand());
            Commands.RegisterCommand(new DefaultLeaveCommand());
        }

        /// <summary>
        /// Registers a command by storing a reference to an instance of it.
        /// </summary>
        /// <param name="command"></param>
        public static void RegisterCommand(ICommand command)
        {
            if (command == null)
                throw new Exception("raft: Cannot register null");
            commandTypes[command.CommandName] = command;
        }

        /// <summary>
        /// Creates a new instance of a command by name and serialized bytes data
        /// </summary>
        /// <param name="name"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ICommand NewCommand(string name, byte[] data)
        {
            // Find the registered command.
            ICommand command = commandTypes[name];
            if (command == null)
            {
                throw new Exception("raft.Command: Unregistered command type:" + name);
            }

            var copy = Activator.CreateInstance(command.GetType());

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(data, 0, data.Length);
                ms.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                (copy as ICommand).Decode(ms);
            }

            return copy as ICommand;
        }
    }
}
