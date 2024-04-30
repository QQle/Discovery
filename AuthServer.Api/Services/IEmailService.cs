using AuthServer.Api.Models;

namespace AuthServer.Api.Services
{
    public interface IEmailService
    {
        void SendEmail(EmailDto request);
    }
}
