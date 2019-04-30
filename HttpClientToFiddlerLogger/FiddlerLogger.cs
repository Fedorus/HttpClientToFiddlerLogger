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
    public class FiddlerLogger : DelegatingHandler, IDisposable
    {
        private string Filename { get; }
        private readonly HttpClientHandler _innerHandler;
        private readonly ZipArchive _archive;
        private readonly FiddlerIndexHtml _fiddlerIndexHtml = new FiddlerIndexHtml();
        private int _index = 0;
        private int Index => Interlocked.Increment(ref _index);

        
        
        public FiddlerLogger(HttpClientHandler innerHandler, string filename) : base(innerHandler)
        {
            innerHandler.AllowAutoRedirect = false;
            _innerHandler = innerHandler;
            if (!filename.EndsWith(".saz"))
            {
                filename = filename + ".saz";
            }
            
            Filename = filename;
            _archive = new ZipArchive(File.Open(Filename, FileMode.Create), ZipArchiveMode.Update);
        }

        ~FiddlerLogger()
        {
            Dispose(false);
        }

        private bool disposed = false;
        protected new void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;
            
            if(disposing)
            {
                this.CloseArchive();
                base.Dispose(disposing);
            }

            disposed = true;
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

            
            if (request.Content != null)
            {
                var content = CodePagesEncodingProvider.Instance.GetEncoding("windows-1252")
                    .GetString(await request.Content.ReadAsByteArrayAsync());
                foreach (var header in request.Content.Headers)
                {
                    sb.AppendLine($"{header.Key}: {string.Join(" ", header.Value)}");
                }
                
                sb.Append(content);
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine();
            }

            var localIndex = Index;

            var entry = _archive.CreateEntry($"raw/{localIndex:D8}_c.txt").Open();
                var byteArray = CodePagesEncodingProvider.Instance.GetEncoding("windows-1252").GetBytes(sb.ToString());
                await entry.WriteAsync(byteArray, 0, byteArray.Length, cancellationToken);
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
                var content = await response.Content.ReadAsByteArrayAsync();
                foreach (var item in response.Content.Headers)
                {
                    sb.AppendLine($"{item.Key}: {string.Join(" ", item.Value)}");
                }
                
                sb.AppendLine();
                logEntry.Body = response.Content?.Headers.ContentLength;
                logEntry.ContentType = response.Content?.Headers.ContentType.ToString();
                logEntry.Caching = response.Headers.CacheControl+"; "+response.Content?.Headers?.Expires;
                
                if (content != null && content.Length>0)
                {
                    sb.Append(CodePagesEncodingProvider.Instance.GetEncoding("windows-1252").GetString(content));
                }
            }

            entry = _archive.CreateEntry($"raw/{localIndex.ToString("D8")}_s.txt").Open();
            byteArray = CodePagesEncodingProvider.Instance.GetEncoding("windows-1252").GetBytes(sb.ToString());
            //byteArray =  Encoding.GetEncoding(1251).GetBytes(sb.ToString());
            await entry.WriteAsync(byteArray, 0, byteArray.Length);
            entry.Close();
            
            _fiddlerIndexHtml.Entries.Add(logEntry);
            if (response.Headers.Location !=null)
            {
                Uri redirectLink = response.Headers.Location.IsAbsoluteUri
                    ? response.Headers.Location
                    : new Uri(new UriBuilder(request.RequestUri.Scheme, request.RequestUri.Host).Uri,  response.Headers.Location.ToString());
                return await this.SendAsync(new HttpRequestMessage(HttpMethod.Get, redirectLink), cancellationToken);
            }
            return response;
        }

        private void CloseArchive()
        {
            var indexFile = _archive.CreateEntry("Index.html").Open();
            {
                //Encoding.Convert(Encoding.Default, Encoding.ASCII, Encoding.Default.GetBytes())
                var bytes = Encoding.ASCII.GetBytes( _fiddlerIndexHtml.ToString());
                indexFile.Write(bytes, 0, bytes.Length);
            }
            indexFile.Close();
            _archive.Dispose();
        }
    }
}