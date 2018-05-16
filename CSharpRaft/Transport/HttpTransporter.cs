using CSharpRaft.Router;
using System;
using System.IO;
using System.Net.Http;
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
        public string AppendEntriesPath
        {
            get
            {
                return this.appendEntriesPath;
            }
        }

        /// <summary>
        ///  Retrieves the RequestVote path.
        /// </summary>
        /// <returns></returns>
        public string RequestVotePath
        {
            get
            {
                return this.requestVotePath;
            }
        }

        /// <summary>
        /// Retrieves the Snapshot path.
        /// </summary>
        /// <returns></returns>
        public string SnapshotPath
        {
            get
            {
                return this.snapshotPath;
            }
        }

        /// <summary>
        /// Retrieves the SnapshotRecovery path.
        /// </summary>
        /// <returns></returns>
        public string SnapshotRecoveryPath
        {
            get
            {
                return this.snapshotRecoveryPath;
            }
        }

        public void Install(Server server, Action<string, IHttpHandler> handler)
        {
            if (server == null || handler == null)
                throw new Exception("Parameter server and router can not be null");

            handler(this.appendEntriesPath, new AppendEntriesHttpHandler(server));

            handler(this.requestVotePath, new RequestVotePathHttpHandler(server));

            handler(this.snapshotPath, new SnapshotPathHttpHandler(server));

            handler(this.snapshotRecoveryPath, new RecoveryPathHttpHandler(server));
        }

        public async Task<AppendEntriesResponse> SendAppendEntriesRequest(Server server, Peer peer, AppendEntriesRequest req)
        {
            try
            {
                Uri rootUrl = new Uri(peer.ConnectionString);
                Uri absoluteUrl = new Uri(rootUrl, this.AppendEntriesPath);
                HttpResponseMessage httpResp;

                using (MemoryStream inSteam = new MemoryStream())
                {
                    req.Encode(inSteam);
                    inSteam.Flush();
                    inSteam.Seek(0, SeekOrigin.Begin);
                    HttpContent content = new StreamContent(inSteam);

                    DebugTrace.TraceLine(server.Name, "POST", absoluteUrl);
                    httpResp = await this.httpClient.PostAsync(absoluteUrl, content);
                }
                
                if (!httpResp.IsSuccessStatusCode)
                {
                    DebugTrace.TraceLine("transporter.SendAppendEntries.response.error:" + httpResp.StatusCode);
                    return null;
                }

                using (MemoryStream outStream = new MemoryStream())
                {
                    await httpResp.Content.CopyToAsync(outStream);
                    outStream.Flush();
                    outStream.Seek(0, SeekOrigin.Begin);

                    AppendEntriesResponse resp = new AppendEntriesResponse();
                    resp.Decode(outStream);
                    return resp;
                }
            }
            catch (Exception err)
            {
                DebugTrace.TraceLine("transporter.SendAppendEntriesRequest.error:" + err);
                return null;
            }
        }
        
        public async Task<RequestVoteResponse> SendVoteRequest(Server server, Peer peer, RequestVoteRequest req)
        {
            try
            {
                Uri rootUrl = new Uri(peer.ConnectionString);
                Uri absoluteUrl = new Uri(rootUrl, this.RequestVotePath);
                HttpResponseMessage httpResp;

                using (MemoryStream inSteam = new MemoryStream())
                {
                    req.Encode(inSteam);

                    HttpContent content = new StreamContent(inSteam);
                    DebugTrace.TraceLine(server.Name, "POST", absoluteUrl);
                    httpResp = await this.httpClient.PostAsync(absoluteUrl, content);
                }

                if (!httpResp.IsSuccessStatusCode)
                {
                    DebugTrace.TraceLine("transporter.SendVote.response.error:" + httpResp.StatusCode);
                    return null;
                }

                using (MemoryStream outStream = new MemoryStream())
                {
                    await httpResp.Content.CopyToAsync(outStream);
                    outStream.Flush();
                    outStream.Seek(0, SeekOrigin.Begin);

                    RequestVoteResponse resp = new RequestVoteResponse();
                    resp.Decode(outStream);
                    return resp;
                }
            }
            catch (Exception err)
            {
                DebugTrace.TraceLine("transporter.SendVoteRequest.error:" + err);
                return null;
            }
        }

        public async Task<SnapshotResponse> SendSnapshotRequest(Server server, Peer peer, SnapshotRequest req)
        {
           
            try
            {
                Uri rootUrl = new Uri(peer.ConnectionString);
                Uri absoluteUrl = new Uri(rootUrl, this.SnapshotPath);
                HttpResponseMessage httpResp;

                using (MemoryStream ms = new MemoryStream())
                {
                    req.Encode(ms);
                    HttpContent content = new StreamContent(ms);

                    DebugTrace.TraceLine(server.Name, "POST", absoluteUrl);
                    httpResp = await this.httpClient.PostAsync(absoluteUrl, content);
                }

                if (!httpResp.IsSuccessStatusCode)
                {
                    DebugTrace.TraceLine("transporter.SendSnapshot.response.error:" + httpResp.StatusCode);
                    return null;
                }

                using (MemoryStream outStream = new MemoryStream())
                {
                    await httpResp.Content.CopyToAsync(outStream);
                    outStream.Flush();
                    outStream.Seek(0, SeekOrigin.Begin);

                    SnapshotResponse resp = new SnapshotResponse();
                    resp.Decode(outStream);
                    return resp;
                }
            }
            catch (Exception err)
            {
                DebugTrace.TraceLine("transporter.SendSnapshotRequest.error:" + err);
                return null;
            }
        }

        public async Task<SnapshotRecoveryResponse> SendSnapshotRecoveryRequest(Server server, Peer peer, SnapshotRecoveryRequest req)
        {
            try
            {
                Uri rootUrl = new Uri(peer.ConnectionString);
                Uri absoluteUrl = new Uri(rootUrl, this.SnapshotRecoveryPath);
                HttpResponseMessage httpResp;

                using (MemoryStream ms = new MemoryStream())
                {
                    req.Encode(ms);

                    DebugTrace.TraceLine(server.Name, "POST", absoluteUrl);

                    HttpContent content = new StreamContent(ms);

                    httpResp = await this.httpClient.PostAsync(absoluteUrl, content);
                }
                if (!httpResp.IsSuccessStatusCode)
                {
                    DebugTrace.TraceLine("transporter.SendSnapshotRecovery.response.error:" + httpResp.StatusCode);
                    return null;
                }

                using (MemoryStream outStream = new MemoryStream())
                {
                    await httpResp.Content.CopyToAsync(outStream);
                    outStream.Flush();
                    outStream.Seek(0, SeekOrigin.Begin);

                    SnapshotRecoveryResponse resp = new SnapshotRecoveryResponse();
                    resp.Decode(outStream);
                    return resp;
                }
            }
            catch (Exception err)
            {
                DebugTrace.TraceLine("transporter.SendSnapshotRecoveryRequest.error:" + err);
                return null;
            }
        }
    }
}
