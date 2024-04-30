using AuthServer.Api.Contextes;
using AuthServer.Api.Models;
using AuthServer.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthServer.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ExtendedIdentityUser> _userManager;

        private readonly UserDbContext _context;

        public UserController(IAuthService authService, UserDbContext context, UserManager<ExtendedIdentityUser> userManager )
        {
            _authService = authService;
            _context = context;
            _userManager = userManager;
        }

        [HttpPost("Registration")]
        public async Task<IActionResult> RegistrationUser([FromBody] RegistrationUser user)
        {
            if (await _authService.Registration(user))
            {
                return Ok("Вы успешно зарегистрировались");
            }
            return BadRequest("что-то пошло не так");
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginUser user)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            var loginResult = await _authService.Login(user);
            var currentUser = await _context.Users
                .Where(x => x.UserName == user.UserName)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            var resultObject = new
            {
                Jwt = loginResult.JwtToken,
                Refresh = loginResult.RefreshToken,
                CurrentUser = currentUser
            };
            if (loginResult.IsLoggedIn)
            {
                return Ok(resultObject);
            }
            return Unauthorized();
        }
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout(string username)
        {
           
            var currentUser = await _userManager.FindByNameAsync(username);
            if (currentUser == null)
            {
                return BadRequest("Пользователь не найден");
            }

            currentUser.RefreshToken = null;
            currentUser.RefreshTokenExpiry = null;

            
            var updateResult = await _userManager.UpdateAsync(currentUser);
            if (!updateResult.Succeeded)
            {
                return StatusCode(500, "Не удалось обновить пользователя");
            }

            return Ok("Вы успешно вышли из системы");
        }

    }
}
