using System.Text.Json;

namespace EasyNetworking.NetCore.Clients.Rest.Models
{
    public class RestOptions
    {
        public BasicAuth? BasicAuth { get; set; }
        public JsonSerializerOptions? RequestJsonSerializerOptions { get; set; }
        public JsonSerializerOptions? ResponseJsonSerializerOptions { get; set; }
    }
}