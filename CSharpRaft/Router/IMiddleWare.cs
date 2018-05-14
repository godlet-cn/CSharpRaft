namespace Router
{
    /// <summary>
    /// IMiddleWare receives an http.Handler and returns another http.Handler.
    /// Middleware can be used to intercept or otherwise modify requests and/or responses
    /// </summary>
    public interface IMiddleWare
    {
        IHttpHandler Middleware(IHttpHandler handler);
    }
}
