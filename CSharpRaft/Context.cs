namespace CSharpRaft
{
    // IContext represents the current state of the server. It is passed into
    // a command when the command is being applied since the server methods
    // are locked.
    public interface IContext
    {
        Server Server { get; }

        int CurrentTerm { get; }

        int CurrentIndex { get; }

        int CommitIndex { get; }
    }

    /// <summary>
    /// Context is the concrete implementation of IContext.
    /// </summary>
    public class Context: IContext
    {
        public Context(Server server, int currentIndex, int currentTerm, int commitIndex)
        {
            this.server = server;
            this.currentIndex = currentIndex;
            this.currentTerm = currentTerm;
            this.commitIndex = commitIndex;
        }

        private Server server;

        private int currentIndex;

        private int currentTerm;

        private int commitIndex;

        /// <summary>
        /// returns a reference to the server.
        /// </summary>
        /// <returns></returns>
        public Server Server
        {
            get
            {
                return this.server;
            }
        }

        /// <summary>
        /// returns current term the server is in.
        /// </summary>
        /// <returns></returns>
        public int CurrentTerm
        {
            get
            {
                return this.currentTerm;
            }
        }

        /// <summary>
        /// returns current index the server is at.
        /// </summary>
        /// <returns></returns>
        public int CurrentIndex
        {
            get
            {
                return this.currentIndex;
            }
        }

        /// <summary>
        /// returns last commit index the server is at.
        /// </summary>
        /// <returns></returns>
        public int CommitIndex
        {
            get
            {
                return this.commitIndex;
            }
        }
    }

}
