using System.Net;

namespace EasyNetworking.NetCore.Clients.Rest.Models
{
    public class RestResponse<T>
    {
        public HttpStatusCode StatusCode { get; set; }
        public string? ResponseString { get; set; }
        public T? ResponseObject { get; set; }
    }
}