using AuthServer.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Api.Services
{
    public interface IAuthService
    {
        Task<bool> Registration(RegistrationUser user);
        Task<LoginResponse> Login(LoginUser user);
        Task<LoginResponse> RefreshToken(RefreshTokenModel model);
       
       
    }
}