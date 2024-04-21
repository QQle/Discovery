using AuthServer.Api.Contextes;
using AuthServer.Api.Models;
using AuthServer.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        private readonly AuthDemoDbContext _context;

        public AuthController(IAuthService authService, AuthDemoDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> RegisterUser([FromBody] LoginUser user)
        {
            if (await _authService.RegisterUser(user))
            {
                return Ok("Successfully done");
            }
            return BadRequest("Something went wrong");
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
            if (loginResult.IsLogedIn)
            {
                return Ok(resultObject);
            }
            return Unauthorized();
        }

        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenModel model)
        {
            var loginResult = await _authService.RefreshToken(model);
            if (loginResult.IsLogedIn)
            {
                return Ok(loginResult);
            }
            return Unauthorized();
        }
    }
}
