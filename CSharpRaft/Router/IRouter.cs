namespace Router
{
    public interface IRouter
    {
         Route HandleFunc(string path, IHttpHandler handler);
    }
}
