using Router;
using System.IO;
using System.Net;

namespace CSharpRaft.Transport
{
    internal class RecoveryPathHttpHandler : AbstractHttpHandler
    {
        private Server server;
        public RecoveryPathHttpHandler(Server server)
        {
            this.server = server;
        }

        public override void Service(HttpListenerRequest req, HttpListenerResponse resp)
        {
            DebugTrace.TraceLine(server.Name, "RECV / Snapshot Recovery");

            SnapshotRecoveryRequest aeReq = new SnapshotRecoveryRequest();

            aeReq.Decode(req.InputStream);

            var aeResp = server.SnapshotRecoveryRequest(aeReq);

            using (Stream output = resp.OutputStream)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    aeResp.Encode(ms);

                    ms.Flush();

                    byte[] data = ms.ToArray();
                    output.Write(data, 0, data.Length);
                }
            }
        }
    }
}