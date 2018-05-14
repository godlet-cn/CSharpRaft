using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

            Commands.RegisterCommand(new NOPCommand());
            Commands.RegisterCommand(new DefaultJoinCommand());
            Commands.RegisterCommand(new DefaultLeaveCommand());
        }

        /// <summary>
        /// Creates a new instance of a command by name and serialized bytes data
        /// </summary>
        /// <param name="name"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Command NewCommand(string name, byte[] data)
        {
            // Find the registered command.
            Command command = commandTypes[name];
            if (command == null)
            {
                throw new Exception("raft.Command: Unregistered command type:" + name);
            }

            var copy = Activator.CreateInstance(command.GetType());
            if (command is CommandEncoder)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(data, 0, data.Length);

                    (copy as CommandEncoder).Decode(ms);
                }    
            }
            else
            {
                string strJson = UTF8Encoding.UTF8.GetString(data);

                copy = JsonConvert.DeserializeObject(strJson, command.GetType());
            }
            return copy as Command;
        }


        /// <summary>
        /// Registers a command by storing a reference to an instance of it.
        /// </summary>
        /// <param name="command"></param>
        public static void RegisterCommand(Command command)
        {
            commandTypes[command.CommandName] = command ?? throw new Exception("raft: Cannot register null");
        }
    }
}
