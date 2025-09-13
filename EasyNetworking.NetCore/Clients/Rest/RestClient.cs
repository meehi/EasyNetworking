using EasyNetworking.NetCore.Clients.Rest.Extensions;
using EasyNetworking.NetCore.Clients.Rest.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EasyNetworking.NetCore.Clients.Rest
{
    public class RestClient(RestOptions? restOptions = null)
    {
        #region Public properties
        public RestOptions? RestOptions { get; private set; } = restOptions;
        #endregion

        #region Public methods
        public async Task<RestResponse<T?>> RequestAsync<T>(string host, object? obj = null)
        {
            RestResponse<T?> result = new();

            HttpClient client = new();
            HttpResponseMessage response;
            if (RestOptions != null && RestOptions.BasicAuth != null)
                if (!string.IsNullOrEmpty(RestOptions.BasicAuth.Username) && !string.IsNullOrEmpty(RestOptions.BasicAuth.Password))
                    client.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(RestOptions.BasicAuth.Username + ":" + RestOptions.BasicAuth.Password)));
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (obj != null)
                response = await client.PostAsync(host, new StringContent(JsonSerializer.Serialize(obj, RestOptions?.RequestJsonSerializerOptions), Encoding.UTF8, "application/json"));
            else
                response = await client.PostAsync(host, new StringContent(""));

            result.StatusCode = response.StatusCode;
            try
            {
                result.ResponseString = await response.Content.ReadAsStringAsync();
            }
            catch
            {
            }

            if (response.StatusCode == HttpStatusCode.OK && !string.IsNullOrEmpty(result.ResponseString))
                result.ResponseObject = result.ResponseString.Deserialize<T>(RestOptions?.ResponseJsonSerializerOptions);

            return result;
        }
        #endregion
    }
}