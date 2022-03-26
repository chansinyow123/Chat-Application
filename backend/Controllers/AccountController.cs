using AutoMapper;
using backend.Data;
using backend.Hubs;
using backend.Models.Account;
using backend.Services;
using backend.Static;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<ChatHub, IChatHub> _chatHub;
        private readonly IEmailService _emailService;
        private readonly IFileService _fileService;
        private readonly IMapper _mapper;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            IHubContext<ChatHub, IChatHub> chatHub,
            IEmailService emailService,
            IFileService fileService,
            IMapper mapper)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _chatHub = chatHub;
            _emailService = emailService;
            _fileService = fileService;
            _mapper = mapper;
        }

        [HttpGet("")]
        [Authorize]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userManager.Users.ToListAsync();

            // return user information ---------------------------------------------------------------
            var userModels = _mapper.Map<List<UserModel>>(users);
            return Ok(userModels);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetUserById([FromRoute] string id)
        {
            // check if user exist -------------------------------------------------------------------
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) 
                return NotFound(new { error = "No user found with this id." });

            // return user information ---------------------------------------------------------------
            var userModel = _mapper.Map<UserModel>(user);
            return Ok(userModel);
        }

        [HttpPost("")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> CreateUser([FromBody] SignUpModel model)
        {
            // Check if user already exist in database ----------------------------------------------
            var userExists = await _userManager.FindByNameAsync(model.Email);  
            if (userExists != null) 
            {
                ModelState.AddModelError(nameof(model.Email), "User already exist.");
                return ValidationProblem(ModelState);  
            }

            // Create User Instance -----------------------------------------------------------------
            ApplicationUser user = new ApplicationUser()
            {
                Name = model.Name,
                Email = model.Email,  
                UserName = model.Email,
            };  

            // Create User in database --------------------------------------------------------------
            var result = await _userManager.CreateAsync(user);  
            if (!result.Succeeded) 
                return StatusCode(StatusCodes.Status406NotAcceptable, new { error = "Create user failed, try again later." });

            // add to ST role -----------------------------------------------------------------------
            await _userManager.AddToRoleAsync(user, UserRoles.ST);

            // Send Email Verification --------------------------------------------------------------
            await _emailService.SendSignUpEmailAsync(user, model.Path);

            // map to userModel DTO
            var userModel = _mapper.Map<UserModel>(user);

            // call to signalr chatHub for account adding
            await _chatHub.Clients.All.AddAccount(userModel);

            // return the userModel DTO
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id, controller = "account" }, userModel);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> UpdateUser([FromRoute] string id, [FromBody] UpdateUserModel model)
        {
            // check if user is exist with id ---------------------------------------------------------------
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(new { error = "No user found with this id." });

            // update user ----------------------------------------------------------------------------------
            user.Name = model.Name;
            var result = await _userManager.UpdateAsync(user);
            
            // return error if update user somehow failed
            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Update user failed, try again later." });

            // map to userModel DTO
            var userModel = _mapper.Map<UserModel>(user);

            // call to signalr chatHuh for account update
            await _chatHub.Clients.All.UpdateAccount(userModel);

            // return updated userModel DTO
            return Ok(userModel);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> DeleteUser([FromRoute] string id)
        {
            // do not allow account to delete itself -----------------------------------------------------------------
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (id == adminId)
                return StatusCode(StatusCodes.Status406NotAcceptable, new { error = "Not allowed delete your own account." });

            // check if username is exist in database ----------------------------------------------------------------
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { error = "No user found with this id." });

            // if there is already a group or private chat for this user, not allow to delete the account ------------
            var hasPrivateChat = await _dbContext.PrivateChat
                    .AsNoTracking()
                    .Where(p => p.SenderId == user.Id || p.ReceiverId == user.Id)
                    .AnyAsync();

            var hasGroup = await _dbContext.GroupUser
                .AsNoTracking()
                .Where(p => p.UserId == user.Id)
                .AnyAsync();

            if (hasPrivateChat || hasGroup) 
                return StatusCode(StatusCodes.Status405MethodNotAllowed, new { error = "Cannot delete this account as there are chat available in this account." });

            // Remove User from database -----------------------------------------------------------------------------
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Delete user failed, try again later." });

            var userModel = _mapper.Map<UserModel>(user);
            return Ok(userModel);
        }

        [HttpPost("upload-image")]
        [Authorize]
        public async Task<IActionResult> UploadImage([FromForm] UploadImageModel model)
        {
            // get the user from database ----------------------------------------------------------------
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null) return NotFound(new { error = "No user found" });

            // If Image is not null, Get the Photo ByteArray and put into database -----------------------
            if (model.File != null)
            {
                // specify the fileSizeLimit --------------------------------------------------------
                long fileSizeLimit = 5242880; // 5mb

                // process the formfile with the image file extension permitted
                var fileByte = await _fileService.ProcessFormFileAsync(model.File, ModelState, FileExtension.Image, fileSizeLimit);
                
                // if error in file, return and display them ---------------------------------------------
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                // save file byte data to database -------------------------------------------------------
                user.File = fileByte;
                user.ContentType = model.File.ContentType;
            }
            else
            {
                // else set the file to null
                user.File = null;
                user.ContentType = null;
            }

            // check if there is any error ---------------------------------------------------------------
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Update image failed, try again later." });

            // map to userModel
            var userModel = _mapper.Map<UserModel>(user);

            return Ok(userModel);
        }

        [HttpPost("toggle-notification")]
        [Authorize]
        public async Task<IActionResult> ToggleNotification()
        {
            // get the user from database ----------------------------------------------------------------
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null) return NotFound(new { error = "No user found" });

            // change data
            user.NeedNotification = !user.NeedNotification;

            // update notification and check if there is any error ---------------------------------------
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Update notification failed, try again later." });

            // map to userModel
            var userModel = _mapper.Map<UserModel>(user);

            // call to signalr chatHuh for account update
            await _chatHub.Clients.All.UpdateAccount(userModel);

            return Ok(userModel);
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            // get the user from database ----------------------------------------------------------------
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null) return NotFound(new { error = "No user found" });

            // map to userModel
            var userModel = _mapper.Map<UserModel>(user);

            return Ok(userModel);
        }

        [HttpGet("image/{accountId}")]
        [Authorize]
        public async Task<IActionResult> GetFile([FromRoute] string accountId)
        {
            // get the account
            var account = await _userManager.FindByIdAsync(accountId);
            if (account == null) return NotFound(new { error = "No such account" });

            // return file
            return File(account.File, account.ContentType);
        }

    }
}
