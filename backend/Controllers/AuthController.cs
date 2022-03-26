using backend.Configs;
using backend.Data;
using backend.Models.Auth;
using backend.Static;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JWTConfig _JWTConfig;

        public AuthController(UserManager<ApplicationUser> userManager, IOptionsSnapshot<JWTConfig> JWTConfig)
        {
            _userManager = userManager;
            _JWTConfig = JWTConfig.Value;
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            // Find if username exist in database ----------------------------------------------------------------------------------------
            var user = await _userManager.FindByNameAsync(loginModel.Username);
            if (user != null && await _userManager.CheckPasswordAsync(user, loginModel.Password))
            {
                // Get User Role ---------------------------------------------------------------------------------------------------------
                var userRoles = await _userManager.GetRolesAsync(user);

                // Initialize Claims -----------------------------------------------------------------------------------------------------
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                // Add Role Claims -------------------------------------------------------------------------------------------------------
                var userRole = userRoles[0];
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));

                // Sign the key using the secret in appsetting.json file -----------------------------------------------------------------
                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_JWTConfig.Secret));

                // Declare jwt token
                var jwtToken = new JwtSecurityToken(
                    issuer: _JWTConfig.ValidIssuer,
                    audience: _JWTConfig.ValidAudience,
                    claims: authClaims,
                    notBefore: DateTime.Now,
                    expires: DateTime.Now.AddDays(_JWTConfig.JWTExpireDays),
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

                // Generate Access Token
                var token = new JwtSecurityTokenHandler().WriteToken(jwtToken);

                // Store all the userinfo into cookie --------------------------------------------------------------------------------------------
                var cookieOptions = new CookieOptions()
                {
                    SameSite = SameSiteMode.Strict,
                    Secure = true,
                    Expires = DateTime.Now.AddDays(_JWTConfig.JWTExpireDays)
                };

                Response.Cookies.Append("jwt_token", token, cookieOptions);
                Response.Cookies.Append("uid", user.Id, cookieOptions);
                Response.Cookies.Append("name", user.Name, cookieOptions);

                // check is this user is an admin or not
                // if isAdmin, store into cookie
                var isAdmin = userRole == UserRoles.Admin;
                if (isAdmin) Response.Cookies.Append("is_admin", "true", cookieOptions);

                // return token and isAdmin
                return Ok();
            }

            return Unauthorized(new { error = "Invalid username or password." });
        }

        //[Authorize]
        //[HttpGet("is-authenticated")]
        //public IActionResult IsAuthenticated()
        //{
        //    var role = User.FindFirst(ClaimTypes.Role).Value;
        //    return Ok(new { role });
        //}

        //[HttpPost("logout")]
        //public IActionResult Logout()
        //{
        //    // Remove all cookies --------------------------------------------------------------------------------
        //    foreach (var cookie in Request.Cookies.Keys)
        //    {
        //        Response.Cookies.Delete(cookie);
        //    }

        //    return Ok(new { success = "Log Out Successfully!" });
        //}
    }
}
