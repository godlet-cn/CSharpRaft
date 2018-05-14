using System;
using System.Net;

namespace Router
{
    public class HttpServer
    {
        private HttpRouter router;

        private HttpListener httpListener;

        public HttpServer()
        {
            router = new HttpRouter();
        }

        /// <summary>
        /// Start http server
        /// </summary>
        public void Start(string hostname,int port)
        {
            string conn = getConnectionString(hostname, port);
            try
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add(conn);
                httpListener.Start();

                httpListener.BeginGetContext(new AsyncCallback(getContextCallBack), httpListener);
            }
            catch (Exception err)
            {
                Console.Error.WriteLine("Failed to start server:" + err.Message + " " + err.StackTrace);
            }
        }

        /// <summary>
        /// Stop http server
        /// </summary>
        public void Stop()
        {
            httpListener.Stop();
        }

        /// <summary>
        /// Bind a http handler
        /// </summary>
        /// <param name="path"></param>
        /// <param name="handler"></param>
        /// <param name="method"></param>
        public void AddHandler(string path,IHttpHandler handler)
        {
            router.HandleFunc(path, handler);
        }

        /// <summary>
        /// Add middlewares for this handlers
        /// </summary>
        /// <param name="middleWares"></param>
        public void AddMiddleWares(params IMiddleWare[] middleWares)
        {
            router.Use(middleWares);
        }

        /// <summary>
        /// Set home page handler
        /// </summary>
        /// <param name="handler"></param>
        public void SetHomePageHandler(IHttpHandler handler)
        {
            router.HomeHandler = handler;
        }

        /// <summary>
        /// Set page error handler
        /// </summary>
        /// <param name="handler"></param>
        public void SetPageErrorHandler(IHttpHandler handler)
        {
            router.NotFoundHandler = handler;
        }

        /// <summary>
        /// Returns the connection string.
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        private string getConnectionString(string hostname, int port)
        {
            return string.Format("http://{0}:{1}/", hostname, port);
        }

        /// <summary>
        /// Handle client request asynchronously
        /// </summary>
        /// <param name="ar"></param>
        private void getContextCallBack(IAsyncResult ar)
        {
            HttpListener httpServer = ar.AsyncState as HttpListener;
            httpServer.BeginGetContext(new AsyncCallback(getContextCallBack), httpServer);

            try
            {
                HttpListenerContext context = httpServer.EndGetContext(ar);
                router.Service(context.Request, context.Response);
            }
            catch (Exception err)
            {
                Console.Error.WriteLine("Fatal:error handle client request:" + err.Message+" "+err.StackTrace);
            }
        }
    }
}
