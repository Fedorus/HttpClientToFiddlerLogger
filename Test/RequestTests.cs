using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Test
{
    public class RequestTests
    {
        private HttpClient _client;
        private CookieContainer _cookies;
        public RequestTests(HttpClient client, CookieContainer cookies)
        {
            _client = client;
            _cookies = cookies;
            _client.BaseAddress = new Uri("https://httpbin.org/");
        }

        public async Task TestAll()
        {
            await HttpRequestsAsync();
            await TestHeadersAndCookiesAsync();
            await TestImagesAsync();
            await TestErrorCodes();
            await TestRedirects();
            await TestPost();
        }

        public async Task TestErrorCodes()
        {
            //await _client.GetAsync("status/100");
            await _client.GetAsync("status/200");
            await _client.GetAsync("status/300");
            await _client.GetAsync("status/400");
            await _client.GetAsync("status/500");
        }

        public async Task HttpRequestsAsync()
        {
            await _client.GetAsync("get");
            await _client.PutAsync("put", null);
            await _client.PatchAsync("patch", null);
            await _client.DeleteAsync("delete");
            await _client.PostAsync("post", null);
        }

        public async Task TestHeadersAndCookiesAsync()
        {
            _client.DefaultRequestHeaders.Add("Custom-Header", "Some value with symbols { \\ 2131238 .,sqwe");
            _cookies.Add(new Cookie("Custom-Cookie", "empty", "/" , "httpbin.org"));
            //await _client.GetAsync("cookies");
            
            const string otherCookieName = "httpClientTest";
            const string otherCookieValue = "testValue";
            await _client.GetAsync($"cookies/set/{otherCookieName}/{otherCookieValue}");
        }

        public async Task TestPost()
        {
            await _client.PostAsync("anything", new StringContent("{ \"status\":\"ok\" }"));
        }

        public async Task TestImagesAsync()
        {
            await _client.GetAsync("image/png");
            var response = await _client.GetAsync("image/jpeg");
            await _client.GetAsync("image/svg");
             await _client.GetAsync("image/webp");

             await _client.PostAsync("anything", new StreamContent(await response.Content.ReadAsStreamAsync()));
        }

        public async Task TestRedirects()
        {
            await _client.GetAsync("absolute-redirect/2");
        }
    }
}