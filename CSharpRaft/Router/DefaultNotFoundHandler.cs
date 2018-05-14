using System;
using System.IO;
using System.Net;
using System.Text;

namespace Router
{
    class DefaultNotFoundHandler : IHttpHandler
    {
        public void Service(HttpListenerRequest request, HttpListenerResponse response)
        {
            PrintRequestMessage(request, response);
        }

        private void PrintRequestMessage(HttpListenerRequest request, HttpListenerResponse response)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("Host: {0}", request.UserHostName);
            Console.WriteLine("{0} {1} HTTP {2}", request.HttpMethod, request.RawUrl, request.ProtocolVersion);
            Console.WriteLine("Accept: {0}", string.Join(",", request.AcceptTypes));
            Console.WriteLine("User-Agent: {0}", request.UserAgent);
            Console.WriteLine("Accept-Encoding: {0}", request.Headers["Accept-Encoding"]);
            Console.WriteLine("Connection: {0}", request.KeepAlive ? "Keep-Alive" : "close");

            if (request.HttpMethod.ToUpper() == "POST")
            {
                //Handle the content of http request 
                ShowRequestData(request);
            }

            string responseString= @"<html><head><title>404</title></head><body><h2>Method not found</h2></body></html>";

            response.ContentLength64 = Encoding.UTF8.GetByteCount(responseString);
            response.ContentType = "text/html; charset=UTF-8";
            Stream output = response.OutputStream;
            StreamWriter writer = new StreamWriter(output);
            writer.Write(responseString);
            writer.Close();
        }

        private void ShowRequestData(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
            {
                Console.WriteLine("No client data was sent with the request.");
                return;
            }

            Stream body = request.InputStream;
            Encoding encoding = request.ContentEncoding;
            StreamReader reader = new StreamReader(body, encoding);

            if (request.ContentType != null)
            {
                Console.WriteLine("Client data content type {0}", request.ContentType);
            }
            Console.WriteLine("Client data content length {0}", request.ContentLength64);

            Console.WriteLine("Start of client data:");

            // Convert the data to a string and display it on the console.
            Console.WriteLine(reader.ReadToEnd());

            Console.WriteLine("End of client data:");

            // If you are finished with the request, it should be closed also.
            body.Close();
            reader.Close();
        }
    }
}
