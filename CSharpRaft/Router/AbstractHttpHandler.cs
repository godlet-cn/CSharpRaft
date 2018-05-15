using System;
using System.IO;
using System.Net;
using System.Text;

namespace CSharpRaft.Router
{
    public abstract class AbstractHttpHandler : IHttpHandler
    {
        private const string METHOD_DELETE = "DELETE";
        private const string METHOD_HEAD = "HEAD";
        private const string METHOD_GET = "GET";
        private const string METHOD_OPTIONS = "OPTIONS";
        private const string METHOD_POST = "POST";
        private const string METHOD_PUT = "PUT";
        private const string METHOD_TRACE = "TRACE";

        public virtual void Service(HttpListenerRequest req, HttpListenerResponse resp)
        {
            string method = req.HttpMethod.ToUpper();
            if (method.Equals(METHOD_GET))
            {
                doGet(req, resp);
            }
            else if (method.Equals(METHOD_HEAD))
            {
                doHead(req, resp);
            }
            else if (method.Equals(METHOD_POST))
            {
                doPost(req, resp);
            }
            else if (method.Equals(METHOD_PUT))
            {
                doPut(req, resp);
            }
            else if (method.Equals(METHOD_DELETE))
            {
                doDelete(req, resp);
            }
            else if (method.Equals(METHOD_OPTIONS))
            {
                doOptions(req, resp);
            }
            else if (method.Equals(METHOD_TRACE))
            {
                doTrace(req, resp);
            }
            else
            {
                throw new Exception("Not supported http method!");
            }
        }

        protected virtual void doHead(HttpListenerRequest req, HttpListenerResponse resp)
        {
            
        }

        protected virtual void doGet(HttpListenerRequest req, HttpListenerResponse resp)
        {
          
        }

        protected virtual void doPost(HttpListenerRequest req, HttpListenerResponse resp)
        {
           
        }

        protected virtual void doPut(HttpListenerRequest req, HttpListenerResponse resp)
        {
           
        }

        protected virtual void doDelete(HttpListenerRequest req, HttpListenerResponse resp)
        {
           
        }

        protected virtual void doOptions(HttpListenerRequest req, HttpListenerResponse resp)
        {
            
        }

        protected virtual void doTrace(HttpListenerRequest req, HttpListenerResponse resp)
        {
            
        }

        protected virtual void WriteString(HttpListenerResponse resp,string responseString)
        {
            if (resp.ContentLength64 != 0) return;
            resp.ContentLength64 = Encoding.UTF8.GetByteCount(responseString);
            resp.ContentType = "text/html; charset=UTF-8";
            Stream output = resp.OutputStream;
            StreamWriter writer = new StreamWriter(output);
            writer.Write(responseString);
            writer.Close();
        }
    }
}
