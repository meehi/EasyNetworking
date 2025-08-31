using EasyNetworking.NetCore.Clients.Rest.Extensions;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EasyNetworking.NetCore.Clients.Rest
{
    public abstract partial class RestClient
    {
        #region Public methods
        public async Task<T?> PostAsync<T>(string host, object? obj = null, string? basicUsername = null, string? basicPassword = null)
        {
            T? result = default;

            string responseString = await RequestAsync(host, obj, basicUsername, basicPassword);
            if (!string.IsNullOrEmpty(responseString))
                try
                {
                    result = responseString.Deserialize<T>();
                }
                catch
                {
                }

            return result;
        }

        public async Task PostAsync(string host, object? obj = null, string? basicUsername = null, string? basicPassword = null)
        {
            await RequestAsync(host, obj, basicUsername, basicPassword);
        }

        public virtual void OnResponseReceived(HttpStatusCode statusCode)
        {
        }
        #endregion

        #region Private methods
        private async Task<string> RequestAsync(string host, object? obj = null, string? basicUsername = null, string? basicPassword = null)
        {
            string result = string.Empty;
            try
            {
                HttpClient client = new();
                HttpResponseMessage response;
                if (!string.IsNullOrEmpty(basicUsername) && !string.IsNullOrEmpty(basicPassword))
                    client.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(basicUsername + ":" + basicPassword)));
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (obj != null)
                    response = await client.PostAsync(host, new StringContent(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json"));
                else
                    response = await client.PostAsync(host, new StringContent(""));

                OnResponseReceived(response.StatusCode);

                if (response.StatusCode == HttpStatusCode.OK)
                    result = await response.Content.ReadAsStringAsync();
            }
            catch
            {
            }

            return result;
        }
        #endregion
    }
}