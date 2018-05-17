using CSharpRaft.Protocol;
using CSharpRaft.Router;
using System.IO;
using System.Net;

namespace CSharpRaft.Transport
{
    internal class SnapshotPathHttpHandler : AbstractHttpHandler
    {
        private Server server;
        public SnapshotPathHttpHandler(Server server)
        {
            this.server = server;
        }

        public override void Service(HttpListenerRequest req, HttpListenerResponse resp)
        {
            DebugTrace.TraceLine(server.Name, "RECV / Snapshot");

            SnapshotRequest aeReq = new SnapshotRequest();

            aeReq.Decode(req.InputStream);

            var aeResp = server.RequestSnapshot(aeReq);

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