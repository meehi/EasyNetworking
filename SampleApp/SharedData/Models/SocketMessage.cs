namespace SharedData.Models
{
    public class SocketMessage
    {
        public string? Message { get; set; }
        public bool ReplyRequired { get; set; }
        public bool ReplySimpleString { get; set; }
    }
}
