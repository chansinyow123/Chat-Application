using AutoMapper;
using backend.Data;
using backend.Hubs;
using backend.Models.Chat;
using backend.Models.Private;
using backend.Services;
using backend.Static;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrivateController : ControllerBase
    {
        private readonly IHubContext<ChatHub, IChatHub> _chatHub;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly IFileService _fileService;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public PrivateController(
            IHubContext<ChatHub, IChatHub> chatHub, 
            UserManager<ApplicationUser> userManager, 
            ApplicationDbContext dbContext, 
            IFileService fileService,
            IMapper mapper,
            INotificationService notificationService)
        {
            _chatHub = chatHub;
            _userManager = userManager;
            _dbContext = dbContext;
            _fileService = fileService;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        [HttpPost("send-message")]
        [Authorize]
        public async Task<IActionResult> SendPrivateMessage([FromForm] SendPrivateMessageModel model) 
        {
            // if message, file, location is all empty, return error
            if (model.File == null && model.Message == null && model.Location == null)
            {
                return NotFound(new { error = "All field is empty" });
            }

            // Get the user own Id
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // find the receiver data
            var receiver = await _userManager.FindByIdAsync(model.ReceiverId);
            if (receiver == null) return NotFound(new { error = "Receiver Id Not Found." });

            // create chat data
            var privateChat = new PrivateChat
            {
                SenderId = userId,
                Receiver = receiver,
                Message = model.Message,
                OnCreate = DateTime.Now,
                OnSeen = (userId == receiver.Id) ? DateTime.Now : null  // if talk to same user, then assign onseen
            };

            // If file is not null, Get the file ByteArray and put into database -------------------------
            if (model.File != null)
            {
                // specify the fileSize limit --------------------------------------------------------
                long fileSizeLimit = 10485760; // 10mb

                // process the formfile with all the file extension permitted
                var fileByte = await _fileService.ProcessFormFileAsync(model.File, ModelState, FileExtension.All, fileSizeLimit);

                // if error in file, return and display them ---------------------------------------------
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                // save file byte data to database -------------------------------------------------------
                privateChat.File = fileByte;
                privateChat.ContentType = model.File.ContentType;
                privateChat.FileName = model.File.FileName;
            }
            else if (model.Location != null)
            {
                // if there is location, assign location
                privateChat.Location = model.Location;
            }

            // store into database
            await _dbContext.PrivateChat.AddAsync(privateChat);
            await _dbContext.SaveChangesAsync();

            // map to messageModel DTO
            var messageModel = _mapper.Map<MessageModel>(privateChat);

            // send to receiver with the userId and DTO
            await _chatHub.Clients.User(receiver.Id).AppendPrivateMessage(userId, messageModel);

            // if userId and receiverId is not the same, then send to sender with the receiverId and DTO
            // this is to prevent two chat will popup whenever a user talk to his/herself.
            // also increment private notification for receiver
            if (userId != receiver.Id)
            {

                // get the sender details
                // and the message details
                var sender = await _userManager.FindByIdAsync(userId);

                // prepare message for sending notification
                var message = _notificationService.GetNotificationMessage(messageModel);

                // prepare a notification message
                var payload = new NotificationPayloadModel
                {
                    SenderName = sender.Name,
                    PrivateId = sender.Id,
                    Message = message
                };

                // convert payload to json
                var payloadJSON = JsonConvert.SerializeObject(payload);

                // send notification to the receiver
                await _notificationService.SendNotificationAsync(receiver.Id, payloadJSON);

                // send message to user
                await _chatHub.Clients.User(userId).AppendPrivateMessage(receiver.Id, messageModel);
                await _chatHub.Clients.User(receiver.Id).IncrementPrivateNotification(userId);
            }

            // return DTO response
            return Ok(messageModel);
        }

        [HttpGet("load-chat")]
        [Authorize]
        public async Task<IActionResult> LoadPrivateChat([FromQuery] int count, [FromQuery] string receiverId)
        {
            // Get the user own Id
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // find the receiver data
            var receiver = await _userManager.FindByIdAsync(receiverId);
            if (receiver == null) return NotFound(new { error = "Receiver not found." });

            // get the privateChat with the limit of 10
            var take = 10;
            var privateChat = await _dbContext.PrivateChat
                .AsNoTracking()
                .Where(p =>
                    ((p.SenderId == userId && p.ReceiverId == receiverId) ||
                    (p.ReceiverId == userId && p.SenderId == receiverId))
                )
                .OrderByDescending(p => p.OnCreate)
                .Skip(count)
                .Take(take)
                .OrderBy(p => p.OnCreate)
                .ToListAsync();

            // map to messageModel DTO
            var messageModel = _mapper.Map<List<MessageModel>>(privateChat);

            // Map to LoadMessageModel DTO
            var model = new LoadMessageModel
            {
                HaveMoreChat = (privateChat.Count >= take),
                Messages = messageModel
            };

            // return the model DTO
            return Ok(model);
        }

        [HttpPost("seen")]
        [Authorize]
        public async Task<IActionResult> SeenPrivateChat([FromBody] SeenPrivateMessageModel model)
        {
            // Get the user own Id
            var receiverId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // if receiverId and senderId is both the same, then do nothing
            // this is because OnSeen has been assigned in "SendPrivateMessage" for both the same user
            if (receiverId == model.SenderId) return Ok();

            // find the receiver data
            var sender = await _userManager.FindByIdAsync(model.SenderId);
            if (sender == null) return NotFound(new { error = "Sender not found" });

            // get the list of private chat with on seen null
            var privateChat = await _dbContext.PrivateChat
                .Where(p =>
                    p.ReceiverId == receiverId &&
                    p.SenderId == sender.Id &&
                    p.OnSeen == null
                )
                .ToListAsync();

            // if there is no chat to seen, then do nothing
            if (privateChat.Count <= 0) return Ok();

            // assign every on seen to todays datetime
            foreach (var c in privateChat)
            {
                c.OnSeen = DateTime.Now;
            }

            // save in db
            await _dbContext.SaveChangesAsync();

            // signalr to these two user to notify them they have seen the message
            await _chatHub.Clients.User(receiverId).PrivateSeen(sender.Id);
            await _chatHub.Clients.User(sender.Id).PrivateSeen(receiverId);

            // return ok status
            return Ok();
        }

        [HttpDelete("delete/{messageId:int}")]
        [Authorize]
        public async Task<IActionResult> DeletePrivateChat([FromRoute] int messageId)
        {
            // Get the user own Id
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // get the privateChat
            var privateChat = await _dbContext.PrivateChat.FindAsync(messageId);
            if (privateChat == null) return NotFound(new { error = "No such private chat" });

            // if it is not sender, not allow them to delete
            if (privateChat.SenderId != userId)
                return StatusCode(StatusCodes.Status406NotAcceptable, new { error = "Not allow to delete this message." });

            // Set todays datetime in OnDelete
            privateChat.OnDelete = DateTime.Now;
            privateChat.Message = null;
            privateChat.Location = null;
            privateChat.File = null;
            privateChat.ContentType = null;
            privateChat.FileName = null;

            // Save the changes
            await _dbContext.SaveChangesAsync();

            // map to messageModel DTO
            var messageModel = _mapper.Map<MessageModel>(privateChat);

            // Call signalr to both sender and receiver to delete the message if available
            await _chatHub.Clients.User(privateChat.SenderId).UpdatePrivateMessage(privateChat.ReceiverId, messageModel);

            // if both senderId and receiverId is not the same, then only call DeletePrivateMessage again
            // this is to prevent delete message twice for the same user
            if (privateChat.SenderId != privateChat.ReceiverId) {
                await _chatHub.Clients.User(privateChat.ReceiverId).UpdatePrivateMessage(privateChat.SenderId, messageModel);
            }

            // return ok status
            return Ok();
        }

        [HttpGet("message")]
        [Authorize]
        public async Task<IActionResult> GetPrivateMessage([FromQuery] string privateId, [FromQuery] int messageId)
        {
            // Get the user own Id
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // get the private chat
            // and also check is info null or not
            var privateChat = await _dbContext.PrivateChat
                .Where(p =>
                    p.Id == messageId &&
                    p.Info == null &&
                    ((p.SenderId == userId && p.ReceiverId == privateId) ||
                    (p.ReceiverId == userId && p.SenderId == privateId))
                )
                .FirstOrDefaultAsync();

            // if privateChat not found
            if (privateChat == null) return NotFound(new { error = "Private chat not found." });

            // prepare the infoPrivateMessageModel
            var model = new InfoPrivateMessageModel
            {
                Message = _mapper.Map<MessageModel>(privateChat),
                OnSeen = privateChat.OnSeen,
            };

            // return the model
            return Ok(model);
        }

        [HttpGet("file/{messageId:int}")]
        [Authorize]
        public async Task<IActionResult> GetFile([FromRoute] int messageId)
        {
            // Get the user own Id
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // get the privateChat
            var privateChat = await _dbContext.PrivateChat.FindAsync(messageId);
            if (privateChat == null) return NotFound(new { error = "No such private chat." });

            // if there is no file, do nothing
            if (privateChat.File == null) return NotFound(new { error = "No such file." });

            // if it is not sender or receiver, not allow them to view
            if (privateChat.SenderId != userId && privateChat.ReceiverId != userId)
                return StatusCode(StatusCodes.Status406NotAcceptable, new { error = "Not allow to view this file." });

            // return file
            return File(privateChat.File, privateChat.ContentType);
        }
    }
}
