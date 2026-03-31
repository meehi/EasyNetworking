namespace EasyNetworking.NetCore.Clients.WebSocketProxy.Models
{
    internal class MessageWrapper
    {
        public Guid? Id { get; set; }
        public string? MessageType { get; set; }
        public object? Message { get; set; }
        public Guid? ReplyId { get; set; }
    }
}