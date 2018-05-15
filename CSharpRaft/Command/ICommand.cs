using System.IO;

namespace CSharpRaft.Command
{
    /// <summary>
    /// ICommand represents an action to be taken on the replicated state machine.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// The name of Command
        /// </summary>
        string CommandName { get; }

        /// <summary>
        /// Apply this command to the server.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        object Apply(IContext context);

        /// <summary>
        /// Serialize command
        /// </summary>
        /// <param name="writer"></param>
        /// <returns></returns>
        bool Encode(Stream writer);

        /// <summary>
        /// Deserialize command
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        bool Decode(Stream reader);
    }

}
