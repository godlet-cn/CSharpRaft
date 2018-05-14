using System.Net;

namespace Router
{
    public class Route
    {
        private IHttpHandler handler;
        private string pattern;

        public Route(string pattern, IHttpHandler handler)
        {
            this.pattern = pattern;
            this.handler = handler;
        }

        public IHttpHandler Handler
        {
            get
            {
                return handler;
            }
        }

        public string Pattern
        {
            get
            {
                return this.pattern;
            }
        }

        internal bool Match(HttpListenerRequest request)
        {
            IHttpHandler handler;
            string pattern;

            string path = request.Url.AbsolutePath;
            if (this.Pattern == path)
            {
                handler = this.Handler;
                pattern = this.Pattern;
                return true;
            }

            return pathMatch(this.Pattern, path);
        }

        private bool pathMatch(string pattern, string path)
        {
            int n = pattern.Length;
            if (pattern[n - 1] != '/')
            {
                return pattern == path;
            }
            return path.Length >= n && path.Substring(0, n) == pattern;
        }
    }
}
