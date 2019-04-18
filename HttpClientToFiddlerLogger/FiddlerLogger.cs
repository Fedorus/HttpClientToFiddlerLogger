using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace HttpClientToFiddlerLogger
{
    public class FiddlerLogger : DelegatingHandler
    {
        public string Filename { get; }
        private readonly HttpClientHandler _innerHandler;
        private readonly ZipArchive _archive;
        private FiddlerIndexHtml _fiddlerIndexHtml = new FiddlerIndexHtml();
        private int _index = 0;
        public int Index => Interlocked.Increment(ref _index);

        public FiddlerLogger(HttpClientHandler innerHandler, string filename) : base(innerHandler)
        {
            _innerHandler = innerHandler;
            if (!filename.EndsWith(".saz"))
            {
                filename = filename + ".saz";
            }
            
            Filename = filename;
            _archive = new ZipArchive(File.Open(Filename, FileMode.OpenOrCreate), ZipArchiveMode.Update);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{request.Method} {request.RequestUri} HTTP/{request.Version}");
            
            foreach (var header in request.Headers)
            {
                sb.AppendLine($"{header.Key}: {string.Join(" ", header.Value)}");
            }

            if (_innerHandler.UseCookies && _innerHandler.CookieContainer.GetCookies(request.RequestUri).Count >0)
            {
                sb.AppendLine("Cookie: " + _innerHandler.CookieContainer.GetCookieHeader(request.RequestUri));
            }
            
            foreach (var item in request.Properties)
            {
                sb.AppendLine($"{item.Key}: {item.Value.ToString()}");
            }

            sb.AppendLine();
            
            if (request.Content != null)
            {
                sb.AppendLine(await request.Content.ReadAsStringAsync());
            }

            var localIndex = Index;

            var entry = _archive.CreateEntry($"raw/{localIndex.ToString("D8")}_c.txt").Open();
                var byteArray = Encoding.Default.GetBytes(sb.ToString());
                await entry.WriteAsync(byteArray, 0, byteArray.Length);
            entry.Close();
            
            var response = await base.SendAsync(request, cancellationToken);
            
            //Console.WriteLine(response.ToString());
            sb = new StringBuilder();
            sb.AppendLine($"HTTP/{response.Version} {(int)response.StatusCode} {response.StatusCode}");
            
            foreach (var header in response.Headers)
            {
                if (header.Key == "Set-Cookie") {
                    sb.AppendLine($"{header.Key}: {string.Join("\r\nSet-Cookie: ", header.Value)}");
                }
                else
                    sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            var logEntry = new LogEntry
            {
                Host = response.RequestMessage.RequestUri.Host,
                Path = response.RequestMessage.RequestUri.PathAndQuery,
                Index = localIndex,
                Protocol = response.RequestMessage.RequestUri.Scheme.ToUpper(),
                StatusCode = (int) response.StatusCode
            };
            if (response.Content !=null)
            {
                var content = await response.Content.ReadAsStringAsync();
                foreach (var item in response.Content.Headers)
                {
                    sb.AppendLine($"{item.Key}: {string.Join(" ", item.Value)}");
                }
                logEntry.Body = response.Content?.Headers.ContentLength;
                logEntry.ContentType = response.Content?.Headers.ContentType.ToString();
                logEntry.Caching = response.Headers.CacheControl.ToString()+"; "+response.Content?.Headers.Expires;
                
                sb.AppendLine("Content-Length: "+response.Content.Headers.ContentLength);
                sb.AppendLine();
                sb.AppendLine(content);
            }

            entry = _archive.CreateEntry($"raw/{localIndex.ToString("D8")}_s.txt").Open();
            byteArray = Encoding.Default.GetBytes(sb.ToString());
            await entry.WriteAsync(byteArray, 0, byteArray.Length);
            entry.Close();
            
            _fiddlerIndexHtml.Entries.Add(logEntry);
            
            return response;
        }

        public void Close()
        {
            var indexFile = _archive.CreateEntry("Index.html").Open();
            {
                var bytes = Encoding.Default.GetBytes( _fiddlerIndexHtml.ToString());
                indexFile.Write(bytes, 0, bytes.Length);
            }
            indexFile.Close();
            _archive.Dispose();
        }
    }
}