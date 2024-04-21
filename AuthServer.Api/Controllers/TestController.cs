using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace AuthServer.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {

        [HttpGet]
        public async Task<IActionResult> Test()
        {
            return  Ok();
        }

        [HttpGet("AuthorizedTest")]
        [Authorize]
        public IActionResult AuthorizedTest()
        {
            var authorizationHeader = this.HttpContext.Request.Headers["Authorization"].FirstOrDefault();

            string jwtTokenString = authorizationHeader.Replace("Bearer ", "");

            var jwt = new JwtSecurityToken(jwtTokenString);

            var response = $"Authenticated!{Environment.NewLine}";

            response += $"{Environment.NewLine}Exp Time: {jwt.ValidTo.ToLongTimeString()}, Time: {DateTime.Now.ToLongTimeString()}";

            return Ok(response);
        }
    }
}
