using System.Text.Json;

namespace EasyNetworking.NetCore.Clients.Rest.Extensions
{
    public static class JsonEx
    {
        public static T? Deserialize<T>(this string jsonText, JsonSerializerOptions? options = null)
        {
            if (string.IsNullOrEmpty(jsonText))
                return default;

            return JsonSerializer.Deserialize<T>(jsonText, options);
        }
    }
}