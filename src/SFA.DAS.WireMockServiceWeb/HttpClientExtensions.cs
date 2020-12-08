using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.WireMockServiceWeb
{
    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, string url, T data)
        {
            return await client.PostAsync(url, data.GetStringContent());
        }

        public static async Task<HttpResponseMessage> PostNothingAsync(this HttpClient client, string url)
        {
            return await client.PostAsync(url, null);
        }

        public static StringContent GetStringContent(this object obj)
            => new StringContent(JsonConvert.SerializeObject(obj), Encoding.Default, "application/json");
    }
}
