using CSharpRaft.Protocol;
using CSharpRaft.Router;
using System.IO;
using System.Net;

namespace CSharpRaft.Transport
{
    class AppendEntriesHttpHandler : AbstractHttpHandler
    {
        private Server server;

        public AppendEntriesHttpHandler(Server server)
        {
            this.server = server;
        }

        public override void Service(HttpListenerRequest req, HttpListenerResponse resp)
        {
            DebugTrace.TraceLine(server.Name, "RECV /appendEntries");

            AppendEntriesRequest aeReq = new AppendEntriesRequest();

            aeReq.Decode(req.InputStream);

            var aeResp = server.AppendEntries(aeReq);

            using (Stream output = resp.OutputStream)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    aeResp.Encode(ms);
                    ms.Flush();
                    ms.Seek(0, SeekOrigin.Begin);

                    byte[] data = ms.ToArray();
                    output.Write(data, 0, data.Length);
                }
            }
        }
    }
}
