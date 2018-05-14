using System.Collections.Generic;
using System.Net;

namespace Router
{
    public class HttpRouter : IHttpHandler, IRouter
    {
        public HttpRouter()
        {
            routes = new List<Route>();
            middleWares = new List<IMiddleWare>();

            this.NotFoundHandler = new DefaultNotFoundHandler();
        }

        /// <summary>
        /// Root path handler
        /// </summary>
        public IHttpHandler HomeHandler { get; set; }

        /// <summary>
        /// The handler when none of the registed handlers can handle the request
        /// </summary>
        public IHttpHandler NotFoundHandler { get; set; }

        private List<Route> routes;

        private List<IMiddleWare> middleWares;

        private object mutex = new object();

        #region MiddleWares
        
        /// <summary>
        /// Appends middlewares to the chain.
        /// Middlewares are executed in the order that they are applied to the Router.
        /// </summary>
        /// <param name="mws"></param>
        public void Use(params IMiddleWare[] mws)
        {
            foreach (var mw in mws)
            {
                this.middleWares.Add(mw);
            }
        }

        #endregion

        /// <summary>
        /// Adds a http handler
        /// </summary>
        /// <param name="path"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public Route HandleFunc(string path, IHttpHandler handler)
        {
            lock (mutex)
            {
                Route route = new Route(path, handler);
                this.routes.Add(route);
                return route;
            }
        }


        /// <summary>
        ///  Dispatches the request to the handler whose pattern most closely matches the request URL.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        public void Service(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (request.Url.AbsolutePath.Equals("/"))
            {
                if (HomeHandler != null)
                {
                    HomeHandler.Service(request, response);
                }
            }

            IHttpHandler handler = Match(request);

            if (handler == null)
            {
                handler = this.NotFoundHandler;
            }

            if (handler != null)
            {
                foreach (var mw in this.middleWares)
                {
                    handler = mw.Middleware(handler);
                }

                handler.Service(request, response);
            }
        }

        private IHttpHandler Match(HttpListenerRequest request)
        {
            lock (mutex)
            {
                foreach (var route in this.routes)
                {
                    if (route.Match(request))
                    {
                        return route.Handler;
                    }
                }
                return null;
            }
        }

    }
}
