using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HttpClientToFiddlerLogger;

namespace Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var clientHandler = new HttpClientHandler {Proxy = new WebProxy("localhost:8888")};
            var cookie = new CookieContainer();
            clientHandler.CookieContainer = cookie;
            using (var logger = new FiddlerLogger(clientHandler, "test"))
            {
                var client = new HttpClient(logger);
                var test = new RequestTests(client, cookie);
                await test.TestAll();
            }
            Console.WriteLine("Test done!");
        }

        static async Task ShortExample()
        {
            using (var logger = new FiddlerLogger(new HttpClientHandler(), "logfileName.saz"))
            {
                var client = new HttpClient(logger);
                await client.GetAsync("example.com");
            }
        }
    }
}