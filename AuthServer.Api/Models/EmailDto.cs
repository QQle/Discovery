namespace AuthServer.Api.Models
{
    public class EmailDto
    {
        public string SendTo { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}
