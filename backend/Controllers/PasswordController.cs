using backend.Data;
using backend.Models.Password;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PasswordController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public PasswordController(UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _userManager = userManager;
            _emailService = emailService;
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel forgotPasswordModel)
        {
            // Check if username exist in database ----------------------------------------------------------------
            var user = await _userManager.FindByNameAsync(forgotPasswordModel.Email);
            if (user == null) 
                return Ok(new { error = "Username not exist in database" });

            // Send Forgot Password Email -------------------------------------------------------------------------
            await _emailService.SendForgotPasswordEmailAsync(user, forgotPasswordModel.Path);

            return Ok(new { success = "Forgot Password Email Sent!" });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            // Check if user id exist in database ----------------------------------------------------------------
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return NotFound(new { error = "No user found with this id." });

            // By default, url query will encode '+' with '%20', therefore we have to decode them using webutility
            // Decode the %20 to space
            model.Token = WebUtility.UrlDecode(model.Token);
            // then Replace the space to '+'
            model.Token = model.Token.Replace(" ", "+");

            // Check if reset password successfully --------------------------------------------------------------
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (!result.Succeeded)
            {
                // loop through error and put into modelstate
                // return NotFound() if it is not password related error, which could be token related error
                foreach (var e in result.Errors)
                    if (e.Code.StartsWith("Password"))
                        ModelState.AddModelError(nameof(model.Password), e.Description);
                    else
                        return NotFound(new { error = e.Description });

                return ValidationProblem(ModelState);
            }

            return Ok(new { success = "Reset Password Successfully!" });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            // Get the logged in user from database ----------------------------------------------------------------
            var username = User.Identity.Name;
            var user = await _userManager.FindByNameAsync(username);

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                // loop through error and put into modelstate
                foreach (var e in result.Errors)
                    if (e.Code == "PasswordMismatch")
                        ModelState.AddModelError(nameof(model.OldPassword), e.Description);
                    else
                        ModelState.AddModelError(nameof(model.NewPassword), e.Description);

                return ValidationProblem(ModelState);
            }

            return Ok(new { success = "Changed Password Successfully!" });
        }
    }
}
