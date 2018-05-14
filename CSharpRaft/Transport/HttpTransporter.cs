using Router;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace CSharpRaft.Transport
{
    public class HttpTransporter : ITransporter
    {
        string appendEntriesPath;
        string requestVotePath;
        string snapshotPath;
        string snapshotRecoveryPath;

        HttpClient httpClient;

        public HttpTransporter()
        {
            appendEntriesPath = "/appendEntries";
            requestVotePath = "/requestVote";
            snapshotPath = "/snapshot";
            snapshotRecoveryPath = "/snapshotRecovery";

            httpClient = new HttpClient();
        }

        /// <summary>
        /// Set timeout 
        /// </summary>
        public TimeSpan Timeout
        {
            get
            {
                return this.httpClient.Timeout;
            }
            set
            {
                this.httpClient.Timeout = value;
            }
        }

        /// <summary>
        ///  Retrieves the AppendEntries path.
        /// </summary>
        /// <returns></returns>
        public string AppendEntriesPath()
        {
            return this.appendEntriesPath;
        }

        /// <summary>
        ///  Retrieves the RequestVote path.
        /// </summary>
        /// <returns></returns>
        public string RequestVotePath()
        {
            return this.requestVotePath;
        }

        /// <summary>
        /// Retrieves the Snapshot path.
        /// </summary>
        /// <returns></returns>
        public string SnapshotPath()
        {
            return this.snapshotPath;
        }

        /// <summary>
        /// Retrieves the SnapshotRecovery path.
        /// </summary>
        /// <returns></returns>
        public string SnapshotRecoveryPath()
        {
            return this.snapshotRecoveryPath;
        }

        public void Install(Server server, Action<string,IHttpHandler> handler)
        {
            if (server == null || handler == null)
                throw new Exception("Parameter server and router can not be null");

            handler(this.appendEntriesPath,new AppendEntriesHttpHandler(server));

            handler(this.requestVotePath, new RequestVotePathHttpHandler(server));

            handler(this.snapshotPath, new SnapshotPathHttpHandler(server));

            handler(this.snapshotRecoveryPath, new RecoveryPathHttpHandler(server));
        }

        public async Task<AppendEntriesResponse> SendAppendEntriesRequest(Server server, Peer peer, AppendEntriesRequest req)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                try
                {
                    req.Encode(ms);

                    string url = Path.Combine(peer.ConnectionString, this.AppendEntriesPath());

                    DebugTrace.TraceLine(server.Name, "POST", url);

                    HttpContent content = new StreamContent(ms);

                    HttpResponseMessage httpResp = await this.httpClient.PostAsync(url, content);
                    if (httpResp.IsSuccessStatusCode)
                    {
                        DebugTrace.TraceLine("transporter.SendAppendEntries.response.error:" + httpResp.StatusCode);
                        return null;
                    }

                    ms.SetLength(0);
                    ms.Seek(0, SeekOrigin.Begin);

                    await httpResp.Content.CopyToAsync(ms);

                    AppendEntriesResponse resp = new AppendEntriesResponse();
                    resp.Decode(ms);
                    return resp;
                }
                catch (Exception err)
                {
                    DebugTrace.TraceLine("transporter.SendAppendEntriesRequest.error:" + err);
                    return null;
                }
            }
        }


        public async Task<RequestVoteResponse> SendVoteRequest(Server server, Peer peer, RequestVoteRequest req)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                try
                {
                    req.Encode(ms);

                    string url = Path.Combine(peer.ConnectionString, this.AppendEntriesPath());

                    DebugTrace.TraceLine(server.Name, "POST", url);

                    HttpContent content = new StreamContent(ms);

                    HttpResponseMessage httpResp = await this.httpClient.PostAsync(url, content);
                    if (httpResp.IsSuccessStatusCode)
                    {
                        DebugTrace.TraceLine("transporter.SendVote.response.error:" + httpResp.StatusCode);
                        return null;
                    }

                    ms.SetLength(0);
                    ms.Seek(0, SeekOrigin.Begin);

                    await httpResp.Content.CopyToAsync(ms);

                    RequestVoteResponse resp = new RequestVoteResponse();
                    resp.Decode(ms);
                    return resp;
                }
                catch (Exception err)
                {
                    DebugTrace.TraceLine("transporter.SendVoteRequest.error:" + err);
                    return null;
                }
            }
        }

        public async Task<SnapshotResponse> SendSnapshotRequest(Server server, Peer peer, SnapshotRequest req)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                try
                {
                    req.Encode(ms);

                    string url = Path.Combine(peer.ConnectionString, this.AppendEntriesPath());

                    DebugTrace.TraceLine(server.Name, "POST", url);

                    HttpContent content = new StreamContent(ms);

                    HttpResponseMessage httpResp = await this.httpClient.PostAsync(url, content);
                    if (httpResp.IsSuccessStatusCode)
                    {
                        DebugTrace.TraceLine("transporter.SendSnapshot.response.error:" + httpResp.StatusCode);
                        return null;
                    }

                    ms.SetLength(0);
                    ms.Seek(0, SeekOrigin.Begin);

                    await httpResp.Content.CopyToAsync(ms);

                    SnapshotResponse resp = new SnapshotResponse();
                    resp.Decode(ms);
                    return resp;
                }
                catch (Exception err)
                {
                    DebugTrace.TraceLine("transporter.SendSnapshotRequest.error:" + err);
                    return null;
                }
            }
        }

        public async Task<SnapshotRecoveryResponse> SendSnapshotRecoveryRequest(Server server, Peer peer, SnapshotRecoveryRequest req)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                try
                {
                    req.Encode(ms);

                    string url = Path.Combine(peer.ConnectionString, this.AppendEntriesPath());

                    DebugTrace.TraceLine(server.Name, "POST", url);

                    HttpContent content = new StreamContent(ms);

                    HttpResponseMessage httpResp = await this.httpClient.PostAsync(url, content);
                    if (httpResp.IsSuccessStatusCode)
                    {
                        DebugTrace.TraceLine("transporter.SendSnapshotRecovery.response.error:" + httpResp.StatusCode);
                        return null;
                    }

                    ms.SetLength(0);
                    ms.Seek(0, SeekOrigin.Begin);

                    await httpResp.Content.CopyToAsync(ms);

                    SnapshotRecoveryResponse resp = new SnapshotRecoveryResponse();
                    resp.Decode(ms);
                    return resp;
                }
                catch (Exception err)
                {
                    DebugTrace.TraceLine("transporter.SendSnapshotRecoveryRequest.error:" + err);
                    return null;
                }
            }
        }
    }
}
