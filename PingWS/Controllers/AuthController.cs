using ChatWS.Models;
using ChatWS.Entities; 
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using PingWS.Services;
using Microsoft.AspNetCore.Authorization;
using PingWS.Models;

namespace ChatWS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserDto request)
        {
            var registeredUser = await authService.RegisterAsync(request);
            if (registeredUser == null)
            {
                return BadRequest("Username is already taken.");
            }
            return Ok(registeredUser);
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(UserDto request)
        {
            var response = await authService.LoginAsync(request);
            if(response == null)
            {
                return BadRequest("Invalid username or password.");
            }
            return Ok(response);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenResponseDto>> RefreshTokens(RefreshTokenRequestDto request)
        {
            var response = await authService.RefreshTokensAsync(request);
            if (response == null || response.RefreshToken == null || response.AccessToken==null)
            {
                return Unauthorized("Invalid refresh token.");
            }
            return Ok(response);
        }

        [Authorize]
        [HttpGet]
        public IActionResult AuthenticatedOnlyEndpoint()
        {
            return Ok("you are authenticated!");
        }

        [Authorize(Roles ="Admin")]
        [HttpGet("admin-only")]
        public IActionResult AdminOnlyEndpoint()
        {
            return Ok("you are admin!");
        }
    }

}
