using System.Net;

namespace CSharpRaft.Router
{
    public delegate void RouteHandler(HttpListenerRequest request, HttpListenerResponse response);

    /// <summary>
    /// IHttpHandler is an interface which can handle http request
    /// </summary>
    public interface IHttpHandler
    {
        /// <summary>
        /// Handle http request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        void Service(HttpListenerRequest request,HttpListenerResponse response);
    }

}
