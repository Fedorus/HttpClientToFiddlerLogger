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
            HttpClientHandler handler = new HttpClientHandler(); 
            handler.CookieContainer = new CookieContainer();
            handler.UseCookies = true;
            var logger = new FiddlerLogger(handler, "1.saz");
            HttpClient client = new HttpClient(logger);
            client.DefaultRequestHeaders.Add("User-Agent","Fuck you");
            
            
            //await client.GetStringAsync("https://www.instagram.com/");
            await client.GetStringAsync("https://www.instagram.com/");
            logger.Close();
            Console.ReadKey();
            Console.WriteLine("Hello World!");
        }
    }
}