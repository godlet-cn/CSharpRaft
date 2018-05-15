using CSharpRaft.Router;
using System.Net;

namespace CSharpRaft.Samples.Handlers
{
    class ReadWriteHttpHandler : AbstractHttpHandler
    {
        private CSharpRaft.Server raftServer;

        public ReadWriteHttpHandler(CSharpRaft.Server raftServer)
        {
            this.raftServer = raftServer;
        }

        protected override void doGet(HttpListenerRequest req, HttpListenerResponse resp)
        {

        }

        protected override void doPost(HttpListenerRequest req, HttpListenerResponse resp)
        {

        }
    }
}
