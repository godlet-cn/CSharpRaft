using CSharpRaft.Router;
using System.IO;
using System.Net;

namespace CSharpRaft.Transport
{
    internal class RequestVotePathHttpHandler : AbstractHttpHandler
    {
        private Server server;
        public RequestVotePathHttpHandler(Server server)
        {
            this.server = server;
        }

        public override void Service(HttpListenerRequest req, HttpListenerResponse resp)
        {
            DebugTrace.TraceLine(server.Name, "RECV / RequestVote");

            RequestVoteRequest aeReq = new RequestVoteRequest();

            aeReq.Decode(req.InputStream);

            var aeResp = server.RequestVote(aeReq);

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