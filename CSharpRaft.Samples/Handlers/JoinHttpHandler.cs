using CSharpRaft.Command;
using CSharpRaft.Router;
using System.Net;

namespace CSharpRaft.Samples.Handlers
{
    class JoinHttpHandler : AbstractHttpHandler
    {
        private CSharpRaft.Server server;

        public JoinHttpHandler(CSharpRaft.Server server)
        {
            this.server = server;
        }

        public override void Service(HttpListenerRequest req, HttpListenerResponse resp)
        {
            try
            {
                DefaultJoinCommand command = new DefaultJoinCommand();
                command.Decode(req.InputStream);

                server.Do(command);
            }
            catch (System.Exception err)
            {
                DebugTrace.DebugLine("JoinHttpHandler.Service.err" + err);
            }
        }
    }
}
