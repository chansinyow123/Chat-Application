using backend.Configs;
using backend.Data;
using backend.Models.Chat;
using backend.Static;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WebPush;

namespace backend.Services
{
    public class NotificationService : INotificationService
    {
        private readonly VapidConfig _vapidConfig;
        private readonly ApplicationDbContext _dbContext;
        private readonly IFileService _fileService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationService(
            IOptionsSnapshot<VapidConfig> vapidConfig, 
            ApplicationDbContext dbContext,
            IFileService fileService,
            UserManager<ApplicationUser> userManager)
        {
            _vapidConfig = vapidConfig.Value;
            _dbContext = dbContext;
            _fileService = fileService;
            _userManager = userManager;
        }

        public string GetNotificationMessage(MessageModel model)
        {
            if (model.Info != null)
            {
                return model.Info;
            }
            else if (model.File != null)
            {
                if (_fileService.CheckFileExtension(model.FileName, FileExtension.Image))
                {
                    return "Sent an image.";
                }
                else if (_fileService.CheckFileExtension(model.FileName, FileExtension.Audio))
                {
                    return "Sent an audio.";
                }
                else if (_fileService.CheckFileExtension(model.FileName, FileExtension.Video))
                {
                    return "Sent a video.";
                }
                else
                {
                    return "Sent a Document.";
                }
            }
            else if (model.Location != null)
            {
                return "Sent a location.";
            }
            else
            {
                return model.Message;
            }
        }

        public async Task SendNotificationAsync(string receiverId, string payloadJSON)
        {
            // check is the receiver has notification turn on or not
            // if the user is not found, or the user did not turn on notification, then do nothing
            var user = await _userManager.FindByIdAsync(receiverId);
            if (user == null || !user.NeedNotification) return;

            // prepare vapid details for later send notification
            var vapidDetails = new VapidDetails(_vapidConfig.Subject, _vapidConfig.PublicKey, _vapidConfig.PrivateKey);

            // get the list of notification devices
            var notification = await _dbContext.Notification.Where(n => n.UserId == receiverId).ToListAsync();

            // send the message to the list of devices for the receiver
            foreach (var n in notification)
            {
                var pushSubscription = new PushSubscription(n.Endpoint, n.P256dh, n.Auth);
                var webPushClient = new WebPushClient();
                try
                {
                    await webPushClient.SendNotificationAsync(pushSubscription, payloadJSON, vapidDetails);
                }
                catch (WebPushException exception)
                {
                    // if is Gone or NotFound, means that the subscription is expired or invalid
                    // then we need to remove the data from the database
                    if (exception.StatusCode == HttpStatusCode.Gone || exception.StatusCode == HttpStatusCode.NotFound)
                    {
                        _dbContext.Notification.Remove(n);
                        await _dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        Console.WriteLine("Http STATUS code" + exception.StatusCode);
                    }
                }
            }
        }
    }
}
