using CSharpRaft.Command;
using CSharpRaft.Router;
using System.Net;
using System.IO;
using System.Text;

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

                using (MemoryStream ms = new MemoryStream())
                {
                    req.InputStream.CopyTo(ms);
                    ms.Flush();
                    ms.Seek(0,SeekOrigin.Begin);

                    command.Decode(ms);

                    server.Do(command);
                }
                resp.StatusCode = (int)HttpStatusCode.OK;
                Stream stream = resp.OutputStream;
                byte[] msg = UTF8Encoding.UTF8.GetBytes("Join succeed");
                stream.Write(msg,0,msg.Length);
                stream.Close();
            }
            catch (System.Exception err)
            {
                resp.StatusCode = (int)HttpStatusCode.BadRequest;
                Stream stream = resp.OutputStream;
                byte[] msg = UTF8Encoding.UTF8.GetBytes("Join faild" + err.Message);
                stream.Write(msg, 0, msg.Length);
                stream.Close();
                DebugTrace.DebugLine("JoinHttpHandler.Service.err" + err);
            }
        }
    }
}
