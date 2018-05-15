using CSharpRaft.Command;
using CSharpRaft.Router;
using System.Net;
using System.IO;
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

                //using (StreamReader sr = new StreamReader(req.InputStream))
                //{
                //    string input = sr.ReadToEnd();
                //    using (MemoryStream ms = new MemoryStream())
                //    {
                       


                //    }
                //}

                using (MemoryStream ms = new MemoryStream())
                {
                    req.InputStream.CopyTo(ms);
                    ms.Flush();
                    ms.Seek(0,SeekOrigin.Begin);

                    command.Decode(ms);

                    server.Do(command);
                }   
            }
            catch (System.Exception err)
            {
                DebugTrace.DebugLine("JoinHttpHandler.Service.err" + err);
            }
        }
    }
}
