using backend.Data;
using backend.Hubs;
using backend.Models.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public ChatController(
            IHubContext<ChatHub, IChatHub> chatHub, 
            UserManager<ApplicationUser> userManager, 
            ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost("subscribe-notification")]
        [Authorize]
        public async Task<IActionResult> SubscribeNotification([FromBody] NotificationModel model)
        {
            // Get the user own Id
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // if found that all data is the same in one of the data in the database
            var dataExist = await _dbContext.Notification
                .Where(n => 
                    n.UserId == userId 
                    && n.Endpoint == model.Endpoint
                    && n.P256dh == model.P256dh
                    && n.Auth == model.Auth
                )
                .FirstOrDefaultAsync();

            // do nothing
            if (dataExist != null) return Ok();

            // prepare notification data
            var notification = new Notification
            {
                UserId = userId,
                Endpoint = model.Endpoint,
                P256dh = model.P256dh,
                Auth = model.Auth,
            };

            // save to database
            await _dbContext.Notification.AddAsync(notification);
            await _dbContext.SaveChangesAsync();

            return Ok();
        }
    }
}
