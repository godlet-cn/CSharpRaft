using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpRaft
{
    // Context represents the current state of the server. It is passed into
    // a command when the command is being applied since the server methods
    // are locked.
    public interface Context
    {
        Server Server();

        int CurrentTerm();

        int CurrentIndex();

        int CommitIndex();

    }

    // context is the concrete implementation of Context.
    public class context: Context
    {
        public Server server;

        public int currentIndex;

        public int currentTerm;

        public int commitIndex;

        /// <summary>
        /// returns a reference to the server.
        /// </summary>
        /// <returns></returns>
        public Server Server()
        {
            return this.server;
        }

        /// <summary>
        /// returns current term the server is in.
        /// </summary>
        /// <returns></returns>
        public int CurrentTerm()
        {
            return this.currentTerm;
        }

        /// <summary>
        /// returns current index the server is at.
        /// </summary>
        /// <returns></returns>
        public int CurrentIndex()
        {
            return this.currentIndex;
        }

        /// <summary>
        /// returns last commit index the server is at.
        /// </summary>
        /// <returns></returns>
        public int CommitIndex()
        {
            return this.commitIndex;
        }
    }

}
