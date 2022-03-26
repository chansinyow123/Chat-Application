using AutoMapper;
using backend.Data;
using backend.Hubs;
using backend.Models.Account;
using backend.Models.Chat;
using backend.Models.Group;
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
using System.Security.Claims;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileService _fileService;
        private readonly IHubContext<ChatHub, IChatHub> _chatHub;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public GroupController(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IFileService fileService,
            IHubContext<ChatHub, IChatHub> chatHub,
            IMapper mapper,
            INotificationService notificationService)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _fileService = fileService;
            _chatHub = chatHub;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        [HttpPost("")]
        [Authorize]
        public async Task<IActionResult> CreateGroup([FromForm] CreateGroupModel model)
        {
            // Get the user own Id
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // check is every userId exist in the database or not ------------------------------------------------
            foreach (var id in model.UsersId)
            {
                // dont allow user to include its own id in this list
                if (id == userId)
                {
                    ModelState.AddModelError(nameof(model.UsersId), "Cannot include your own id in this list.");
                    return ValidationProblem(ModelState);
                }

                // check if this id exist in our database account
                var account = await _userManager.FindByIdAsync(id);
                if (account == null) 
                {
                    ModelState.AddModelError(nameof(model.UsersId), "No such userId in the database.");
                    return ValidationProblem(ModelState);
                }
            }

            // prepare new group data --------------------------------------------------------------------------
            var group = new Group
            {
                Name = model.GroupName
            };

            // If File is not null, Get the Photo ByteArray and put into database
            if (model.File != null)
            {
                // specify the fileSizeLimit --------------------------------------------------------
                long fileSizeLimit = 5242880; // 5mb

                // process the formfile with the image file extension permitted
                var fileByte = await _fileService.ProcessFormFileAsync(model.File, ModelState, FileExtension.Image, fileSizeLimit);

                // if error in file, return and display them ----------------------------------------
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                // save file byte data to database --------------------------------------------------
                group.File = fileByte;
                group.ContentType = model.File.ContentType;
            }

            // Add the group into database
            await _dbContext.Group.AddAsync(group);

            // add the list of account to the group ----------------------------------------------------------------------
            foreach (var id in model.UsersId)
            {
                var groupUser = new GroupUser
                {
                    Group = group,
                    UserId = id,
                    IsAdmin = false,
                    OnDelete = null,
                };
                await _dbContext.GroupUser.AddAsync(groupUser);
            }

            // assign this user as admin into this group
            var groupAdminUser = new GroupUser
            {
                Group = group,
                UserId = userId,
                IsAdmin = true,
                OnDelete = null,
            };
            await _dbContext.GroupUser.AddAsync(groupAdminUser);

            // Add info message to group chat ----------------------------------------------------------------------------
            // But first get the user's name who created this group
            var adminUser = await _userManager.FindByIdAsync(userId);
            var groupChat = new GroupChat
            {
                Group = group,
                SenderId = null,
                OnCreate = DateTime.Now,
                Info = $"{adminUser.Name} created this group \"{group.Name}\"",
            };
            await _dbContext.GroupChat.AddAsync(groupChat);

            // make the groupchat to be seen by this user ----------------------------------------------------------------
            var groupChatSeen = new GroupChatSeen
            {
                GroupChat = groupChat,
                UserId = userId,
                OnSeen = DateTime.Now,
            };
            await _dbContext.GroupChatSeen.AddAsync(groupChatSeen);

            // Save the database changes ---------------------------------------------------------------------------------
            await _dbContext.SaveChangesAsync();

            // prepare group model info ----------------------------------------------------------------------------------
            var groupModel = _mapper.Map<GroupModel>(group);
            groupModel.IsExit = false;

            // prepare list of usersId exist in this group
            var listOfGroupUserId = new List<string>(model.UsersId) { userId };

            // Create group chat to these users
            await _chatHub.Clients.Users(listOfGroupUserId).CreateGroupChat(groupModel);

            // prepare messageModel --------------------------------------------------------------------------------------
            var messageModel = _mapper.Map<MessageModel>(groupChat);

            // append the message to all of the user in this group
            await _chatHub.Clients.Users(listOfGroupUserId).AppendGroupMessage(group.Id, messageModel);

            // increment notification count to other user in this group (except this user)
            await _chatHub.Clients.Users(model.UsersId).IncrementGroupNotification(group.Id);

            // prepare to send notification to other user in this group (except this user) -------------------------------
            // prepare a notification message
            var payload = new NotificationPayloadModel
            {
                SenderName = group.Name,
                GroupId = group.Id,
                Message = groupChat.Info
            };

            // convert payload to json
            var payloadJSON = JsonConvert.SerializeObject(payload);

            // loop through other list of user in this grouo and send the notification
            foreach (var id in model.UsersId)
            {
                await _notificationService.SendNotificationAsync(id, payloadJSON);
            }

            // return Ok Status
            return Ok(group.Id);
        }

        [HttpPost("send-message")]
        [Authorize]
        public async Task<IActionResult> SendGroupMessage([FromForm] SendGroupMessageModel model)
        {
            // if message, file, location is all empty, return error -------------------------------------------------------
            if (model.File == null && model.Message == null && model.Location == null)
            {
                return NotFound(new { error = "All field is empty" });
            }

            // Get the user own Id
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // find the group ----------------------------------------------------------------------------------------------
            var group = await _dbContext.Group
                .Include(x => x.GroupUsers)
                .SingleOrDefaultAsync(x => x.Id == model.GroupId);
            if (group == null) return NotFound(new { error = "No such group." });

            // check is this user is within this group or not --------------------------------------------------------------
            // and also check is the user exited this group or not
            var groupUser = await _dbContext.GroupUser
                .Include(x => x.User)
                .SingleOrDefaultAsync(x => 
                    x.GroupId == group.Id 
                    && x.UserId == userId
                    && x.OnDelete == null
                );
            if (groupUser == null) return StatusCode(StatusCodes.Status405MethodNotAllowed, new { error = "Not allow to send the group chat." });

            // create chat data
            var groupChat = new GroupChat
            {
                SenderId = userId,
                Group = group,
                Message = model.Message,
                OnCreate = DateTime.Now,
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
                groupChat.File = fileByte;
                groupChat.ContentType = model.File.ContentType;
                groupChat.FileName = model.File.FileName;
            }
            else if (model.Location != null)
            {
                // if there is location, assign location
                groupChat.Location = model.Location;
            }

            // also assign the message is seen by this user ------------------------------------------------------------
            var groupChatSeen = new GroupChatSeen
            {
                GroupChat = groupChat,
                UserId = userId,
                OnSeen = DateTime.Now,
            };

            // store into database and save database changes
            await _dbContext.GroupChatSeen.AddAsync(groupChatSeen);
            await _dbContext.SaveChangesAsync();

            // map to messageModel DTO --------------------------------------------------------------------------------
            var messageModel = _mapper.Map<MessageModel>(groupChat);

            // prepare a list of group user id exist in this group
            var listOfGroupUserId = group.GroupUsers
                .Where(x => x.OnDelete == null) 
                .Select(x => x.UserId);

            // send message to these users of the group
            await _chatHub.Clients.Users(listOfGroupUserId).AppendGroupMessage(group.Id, messageModel);

            // prepare message for sending notification ---------------------------------------------------------------
            var message = _notificationService.GetNotificationMessage(messageModel);

            // prepare a notification message
            var payload = new NotificationPayloadModel
            {
                SenderName = group.Name,
                GroupId = group.Id,
                Message = $"{groupUser.User.Name}: {message}"
            };

            // convert payload to json
            var payloadJSON = JsonConvert.SerializeObject(payload);

            // get the list of user (except this user) exist in this group
            // loop through them and send the notification
            var otherListOfGroupUserId = listOfGroupUserId.Where(x => x != userId);
            foreach (var id in otherListOfGroupUserId)
            {
                await _notificationService.SendNotificationAsync(id, payloadJSON);
            }
            
            // increment notification to users (except this user) -------------------------------------------------------
            await _chatHub.Clients.Users(otherListOfGroupUserId).IncrementGroupNotification(group.Id);

            // return DTO response
            return Ok(messageModel);
        }

        [HttpGet("load-chat")]
        [Authorize]
        public async Task<IActionResult> LoadGroupChat([FromQuery] int count, [FromQuery] int groupId)
        {
            // Get the user own Id -----------------------------------------------------------------------------------
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // find the group
            var group = await _dbContext.Group
                .AsNoTracking()
                .Include(x => x.GroupUsers)
                .SingleOrDefaultAsync(x => x.Id == groupId);
            if (group == null) return NotFound(new { error = "No such group." });

            // check is this user is within this group or not -------------------------------------------------------
            var groupUser = await _dbContext.GroupUser
                .SingleOrDefaultAsync(x => 
                    x.GroupId == groupId 
                    && x.UserId == userId
                );
            if (groupUser == null) return StatusCode(StatusCodes.Status405MethodNotAllowed, new { error = "Not allow to see the group chat." });

            // get the groupChat with the limit of 10 ---------------------------------------------------------------
            var take = 10;
            var groupChats = await _dbContext.GroupChat
                .AsNoTracking()
                .Where(x =>
                    ((groupUser.OnDelete == null) || (x.OnCreate <= groupUser.OnDelete))
                    && x.GroupId == groupUser.GroupId
                )
                .OrderByDescending(p => p.OnCreate)
                .Skip(count)
                .Take(take)
                .OrderBy(p => p.OnCreate)
                .ToListAsync();

            // map to list of messageModel -------------------------------------------------------------------------
            var groupMessages = _mapper.Map<List<MessageModel>>(groupChats);

            // foreach the groupMessage to assign onSeen
            foreach (var message in groupMessages)
            {
                // get the list of onseen Count and compare to the group of user that is not exited yet
                // if onSeenCount is equal and larger to groupUser, then its means the message was seen
                var OnSeenCount = await _dbContext.GroupChatSeen.CountAsync(x => x.GroupChatId == message.Id);
                message.OnSeen = (OnSeenCount >= group.GroupUsers.Count(x => x.OnDelete == null));
            }

            // Map to LoadMessageModel DTO -------------------------------------------------------------------------
            var model = new LoadMessageModel
            {
                HaveMoreChat = (groupChats.Count >= take),
                Messages = groupMessages
            };

            // return the model DTO
            return Ok(model);
        }

        [HttpPost("seen")]
        [Authorize]
        public async Task<IActionResult> SeenGroupChat([FromBody] SeenGroupMessageModel model)
        {
            // Get the user own Id ---------------------------------------------------------------------------------
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // find the group
            var group = await _dbContext.Group
                .Include(x => x.GroupUsers)
                .SingleOrDefaultAsync(x => x.Id == model.GroupId);
            if (group == null) return NotFound(new { error = "No such group." });

            // check is this user is within this group or not ------------------------------------------------------
            var groupUser = await _dbContext.GroupUser
                .SingleOrDefaultAsync(x => 
                    x.GroupId == group.Id 
                    && x.UserId == userId
                );
            if (groupUser == null) return StatusCode(StatusCodes.Status405MethodNotAllowed, new { error = "Not allow to seen the group chat." });

            // get the list of group chat that is no on seen yet --------------------------------------------------
            var groupChat = await _dbContext.GroupChat
                .Where(x =>
                    ((groupUser.OnDelete == null) || (x.OnCreate <= groupUser.OnDelete))
                    && x.GroupId == group.Id
                    && !_dbContext.GroupChatSeen
                         .Where(y => y.UserId == userId)
                         .Select(y => y.GroupChatId)
                         .Contains(x.Id)
                )
                .ToListAsync();

            // if there is no chat to seen, then do nothing 
            if (groupChat.Count <= 0) return Ok();

            // assign every on seen to todays datetime -------------------------------------------------------------
            foreach (var c in groupChat)
            {
                var groupChatSeen = new GroupChatSeen
                {
                    GroupChatId = c.Id,
                    UserId = userId,
                    OnSeen = DateTime.Now
                };
                await _dbContext.GroupChatSeen.AddAsync(groupChatSeen);
            }

            // save in db
            await _dbContext.SaveChangesAsync();

            // if the last message is seen by everyone -------------------------------------------------------------
            // then signalr to these user of the group to seen the message
            // first get the list of group user exist in this group
            var listOfGroupUser = group.GroupUsers;

            // get the last message of this group
            var lastMessage = await _dbContext.GroupChat
                .AsNoTracking()
                .Include(x => x.GroupChatSeens)
                .Where(x => x.GroupId == group.Id)
                .OrderByDescending(x => x.OnCreate)
                .FirstOrDefaultAsync();

            // if the number of last seen message is larger and equal to the list of undeleted group user
            // signalr to these users to notify that the messages was seen
            if (lastMessage.GroupChatSeens.Count >= listOfGroupUser.Count(x => x.OnDelete == null))
            {
                var listOfGroupUserId = listOfGroupUser.Select(x => x.UserId);
                await _chatHub.Clients.Users(listOfGroupUserId).GroupSeen(group.Id);
            }

            // return ok status
            return Ok();
        }

        [HttpPut("exit/{groupId:int}")]
        [Authorize]
        public async Task<IActionResult> ExitGroupChat([FromRoute] int groupId)
        {
            // Get the user own Id --------------------------------------------------------------------------
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // find the group
            var group = await _dbContext.Group
                .Include(x => x.GroupUsers)
                .SingleOrDefaultAsync(x => x.Id == groupId);
            if (group == null) return NotFound(new { error = "No such group." });

            // check is this user is within this group or not
            // and also check is this user has been deleted or not ------------------------------------------
            var groupUser = await _dbContext.GroupUser
                .Include(x => x.User)
                .SingleOrDefaultAsync(x =>
                    x.GroupId == groupId
                    && x.UserId == userId
                    && x.OnDelete == null
                );
            if (groupUser == null) return NotFound(new { error = "Not allow to exit this group." });

            // Append group info ----------------------------------------------------------------------------
            var groupChat = new GroupChat
            {
                GroupId = groupId,
                SenderId = null,
                Info = $"{groupUser.User.Name} exited the group",
                OnCreate = DateTime.Now,
            };
            await _dbContext.GroupChat.AddAsync(groupChat);

            // exit the user --------------------------------------------------------------------------------
            groupUser.OnDelete = DateTime.Now;
            groupUser.IsAdmin = false;

            // Seen the message -----------------------------------------------------------------------------
            var groupChatSeen = new GroupChatSeen
            {
                GroupChat = groupChat,
                UserId = userId,
                OnSeen = DateTime.Now,
            };
            await _dbContext.GroupChatSeen.AddAsync(groupChatSeen);

            // if there is no admin -------------------------------------------------------------------------
            // and the number of undeleted member in this group is larger than 0
            // then randomly assign admin
            var random = new Random();
            var adminCount = group.GroupUsers.Count(x => x.IsAdmin == true);
            var listOfMember = group.GroupUsers.Where(x => x.OnDelete == null);

            if (adminCount <= 0 && listOfMember.Count() > 0)
            {
                int index = random.Next(listOfMember.Count());
                listOfMember.ElementAtOrDefault(index).IsAdmin = true;
            }

            // Save database changes ------------------------------------------------------------------------
            await _dbContext.SaveChangesAsync();

            // map to messageModel DTO ----------------------------------------------------------------------
            var messageModel = _mapper.Map<MessageModel>(groupChat);

            // prepare a list of group user id that exist in this group
            // (does not include this userId as this user is exited)
            var listOfGroupUserId = group.GroupUsers
                .Where(x => x.OnDelete == null)
                .Select(x => x.UserId);

            // send message to these users of the group
            await _chatHub.Clients.Users(listOfGroupUserId).AppendGroupMessage(groupId, messageModel);
            // send message to this user because the listOfGroupUserId will not include this userid
            await _chatHub.Clients.User(userId).AppendGroupMessage(groupId, messageModel);

            // update this user's group with groupModel ----------------------------------------------------
            var groupModel = _mapper.Map<GroupModel>(group);
            groupModel.IsExit = true;
            await _chatHub.Clients.User(userId).UpdateGroup(groupModel);

            // prepare a notification message ---------------------------------------------------------------
            var payload = new NotificationPayloadModel
            {
                SenderName = group.Name,
                GroupId = group.Id,
                Message = messageModel.Info,
            };

            // convert payload to json
            var payloadJSON = JsonConvert.SerializeObject(payload);

            // get the list of user (except this user) exist in this group
            // loop through them and send the notification
            var otherListOfGroupUserId = listOfGroupUserId.Where(x => x != userId);
            foreach (var id in otherListOfGroupUserId)
            {
                await _notificationService.SendNotificationAsync(id, payloadJSON);
            }

            // increment notification to users (except this user) ------------------------------------------
            await _chatHub.Clients.Users(otherListOfGroupUserId).IncrementGroupNotification(group.Id);

            return Ok();
        }

        [HttpPost("remove")]
        [Authorize]
        public async Task<IActionResult> RemoveGroupUser(UpdateGroupUserModel model)
        {
            // Get the user own Id --------------------------------------------------------------------------
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // find the group
            var group = await _dbContext.Group
                .Include(x => x.GroupUsers)
                .SingleOrDefaultAsync(x => x.Id == model.GroupId);
            if (group == null) return NotFound(new { error = "No such group." });

            // check is this user is within this group or not -----------------------------------------------
            // and also check is this user has been deleted or not
            // and also check is this user is admin or not
            var groupUser = await _dbContext.GroupUser
                .Include(x => x.User)
                .SingleOrDefaultAsync(x =>
                    x.GroupId == model.GroupId
                    && x.UserId == userId
                    && x.OnDelete == null
                    && x.IsAdmin == true
                );
            if (groupUser == null) return NotFound(new { error = "Not allow to remove user as you are not an admin." });

            // check is userid exist in this group or not --------------------------------------------------
            var removeUser = await _dbContext.GroupUser
                .Include(x => x.User)
                .SingleOrDefaultAsync(x =>
                    x.GroupId == model.GroupId
                    && x.UserId == model.UserId
                    && x.OnDelete == null
                );
            if (removeUser == null) return NotFound(new { error = "UserId not exist." });

            // Append group info ----------------------------------------------------------------------------
            var groupChat = new GroupChat
            {
                GroupId = model.GroupId,
                SenderId = null,
                Info = $"{groupUser.User.Name} removed {removeUser.User.Name}",
                OnCreate = DateTime.Now,
            };
            await _dbContext.GroupChat.AddAsync(groupChat);

            // exit the user --------------------------------------------------------------------------------
            removeUser.OnDelete = DateTime.Now;
            removeUser.IsAdmin = false;

            // This user will seen the message --------------------------------------------------------------
            var groupChatSeen = new GroupChatSeen
            {
                GroupChat = groupChat,
                UserId = userId,
                OnSeen = DateTime.Now,
            };
            await _dbContext.GroupChatSeen.AddAsync(groupChatSeen);

            // Save database changes
            await _dbContext.SaveChangesAsync();

            // map to messageModel DTO ----------------------------------------------------------------------
            var messageModel = _mapper.Map<MessageModel>(groupChat);

            // prepare a list of group user id that exist in this group
            // (does not include removeUserId as this the user is removed)
            var listOfGroupUserId = group.GroupUsers
                .Where(x => x.OnDelete == null)
                .Select(x => x.UserId);

            // send message to these users of the group
            await _chatHub.Clients.Users(listOfGroupUserId).AppendGroupMessage(group.Id, messageModel);
            // send message to removeUserId because the listOfGroupUserId will not include this userid
            await _chatHub.Clients.User(removeUser.UserId).AppendGroupMessage(group.Id, messageModel);

            // update this user's group with groupModel ----------------------------------------------------
            var groupModel = _mapper.Map<GroupModel>(group);
            groupModel.IsExit = true;
            await _chatHub.Clients.User(removeUser.UserId).UpdateGroup(groupModel);

            // prepare a notification message --------------------------------------------------------------
            var payload = new NotificationPayloadModel
            {
                SenderName = group.Name,
                GroupId = group.Id,
                Message = messageModel.Info,
            };

            // convert payload to json
            var payloadJSON = JsonConvert.SerializeObject(payload);

            // get the list of user (except this user) exist in this group
            // loop through them and send the notification
            var otherListOfGroupUserId = listOfGroupUserId.Where(x => x != userId);
            foreach (var id in otherListOfGroupUserId)
            {
                await _notificationService.SendNotificationAsync(id, payloadJSON);
            }

            // increment notification to users (except this user) ------------------------------------------
            await _chatHub.Clients.Users(otherListOfGroupUserId).IncrementGroupNotification(group.Id);

            return Ok();
        }

        //[HttpPost("add")]
        //[Authorize]
        //public async Task<IActionResult> AddGroupUser(AddGroupUserModel model)
        //{
        //    // Get the user own Id
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        //    // find the group
        //    var group = await _dbContext.Group
        //        .Include(x => x.GroupUsers)
        //        .SingleOrDefaultAsync(x => x.Id == model.GroupId);
        //    if (group == null) return NotFound(new { error = "No such group." });

        //    // check is this user is within this group or not
        //    // and also check is this user has been deleted or not
        //    // and also check is this user is an admin or not
        //    var groupUser = await _dbContext.GroupUser
        //        .Include(x => x.User)
        //        .SingleOrDefaultAsync(x =>
        //            x.GroupId == model.GroupId
        //            && x.UserId == userId
        //            && x.OnDelete == null
        //            && x.IsAdmin == true
        //        );
        //    if (groupUser == null) return NotFound(new { error = "Not allow to add user as you are not admin." });

        //    // prepare a list of username added
        //    // this is to later append groupChat after the user is added
        //    var userAdded = new List<string>();

        //    // prepare a list of new UserId that is newly added
        //    var newUserId = new List<string>();
        //    // prepare a list of old UserId that is already inside the group
        //    var oldUserId = new List<string>();

        //    // foreach userIds
        //    foreach (var id in model.UserIds)
        //    {
        //        // check if userIds is exist within database --------------------------------------------------
        //        // if dont have, return not found
        //        var user = await _userManager.FindByIdAsync(id);
        //        if (user == null) return NotFound(new { error = "No such userId" });

        //        // add user's name to userAdded
        //        userAdded.Add(user.Name);

        //        // check if this user is already exist in this group or not -----------------------------------
        //        var groupUserExist = await _dbContext.GroupUser
        //            .SingleOrDefaultAsync(x =>
        //                x.GroupId == model.GroupId
        //                && x.UserId == id
        //            );

        //        // if exist, then assign on delete to null
        //        // and continue to loop
        //        if (groupUserExist != null)
        //        {
        //            groupUserExist.OnDelete = null;
        //            oldUserId.Add(id);
        //            continue;
        //        }

        //        // if not exist, add to group ----------------------------------------------------------------
        //        var addGroupUser = new GroupUser
        //        {
        //            GroupId = model.GroupId,
        //            UserId = id,
        //            IsAdmin = false,
        //            OnDelete = null,
        //        };
        //        await _dbContext.GroupUser.AddAsync(addGroupUser);

        //        // add to newUserId
        //        newUserId.Add(id);
        //    }

        //    // append group chat --------------------------------------------------------------------------
        //    // but first join the added user's name to string
        //    var userAddedString = String.Join(", ", userAdded);
        //    var groupChat = new GroupChat
        //    {
        //        GroupId = model.GroupId,
        //        SenderId = null,
        //        Info = $"{groupUser.User.Name} Added {userAddedString}",
        //        OnCreate = DateTime.Now,
        //    };
        //    await _dbContext.GroupChat.AddAsync(groupChat);

        //    // Save database changes ------------------------------------------------------------------------
        //    await _dbContext.SaveChangesAsync();


        //    // prepare group model info ---------------------------------------------------------------------
        //    // this is to create groupchat to newUserid and also update to oldUserId
        //    var groupModel = _mapper.Map<GroupModel>(group);
        //    groupModel.IsExit = false;

        //    // Create group chat to new user
        //    await _chatHub.Clients.Users(newUserId).CreateGroupChat(groupModel);
        //    // update group chat to old user
        //    await _chatHub.Clients.Users(oldUserId).UpdateGroup(groupModel);
        //    // clear group message to old user
        //    await _chatHub.Clients.Users(oldUserId).ClearGroupMessage(group.Id);
            

        //    // get the list of groupUserId exist in this group ----------------------------------------------
        //    var listOfGroupUserId = group.GroupUsers
        //        .Where(x => x.OnDelete == null)
        //        .Select(x => x.UserId);

        //    // map the groupchat to messageModel
        //    var messageModel = _mapper.Map<MessageModel>(groupChat);
           
        //    await _chatHub.Clients.Users(listOfGroupUserId).AppendGroupMessage(group.Id, messageModel);

        //    // prepare a notification message ---------------------------------------------------------------
        //    var payload = new NotificationPayloadModel
        //    {
        //        SenderName = group.Name,
        //        GroupId = group.Id,
        //        Message = messageModel.Info,
        //    };

        //    // convert payload to json
        //    var payloadJSON = JsonConvert.SerializeObject(payload);

        //    // get the list of user (except this user) exist in this group
        //    // loop through them and send the notification
        //    var otherListOfGroupUserId = listOfGroupUserId.Where(x => x != userId);
        //    foreach (var id in otherListOfGroupUserId)
        //    {
        //        await _notificationService.SendNotificationAsync(id, payloadJSON);
        //    }

        //    // increment notification to users (except this user) ------------------------------------------
        //    await _chatHub.Clients.Users(otherListOfGroupUserId).IncrementGroupNotification(group.Id);

        //    return Ok();
        //}

        [HttpDelete("delete/{messageId:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteGroupChat([FromRoute] int messageId)
        {
            // Get the user own Id ------------------------------------------------------------------------------------------
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // find the group chat
            var groupChat = await _dbContext.GroupChat.FindAsync(messageId);
            if (groupChat == null) return NotFound(new { error = "No such group chat." });

            // check is this user is within this group or not --------------------------------------------------------------
            var groupUser = await _dbContext.GroupUser
                .AsNoTracking()
                .SingleOrDefaultAsync(x => 
                    x.GroupId == groupChat.GroupId 
                    && x.UserId == userId
                    && x.OnDelete == null
                );
            if (groupUser == null) return NotFound(new { error = "Not allow to delete this group chat." });

            // if it is not sender, not allow them to delete ---------------------------------------------------------------
            if (groupChat.SenderId != userId)
                return StatusCode(StatusCodes.Status406NotAcceptable, new { error = "Not allow to delete this message." });

            // Set todays datetime in OnDelete -----------------------------------------------------------------------------
            groupChat.OnDelete = DateTime.Now;
            groupChat.Message = null;
            groupChat.Location = null;
            groupChat.File = null;
            groupChat.ContentType = null;
            groupChat.FileName = null;

            // Save the changes
            await _dbContext.SaveChangesAsync();

            // map to messageModel DTO ------------------------------------------------------------------------------------
            var messageModel = _mapper.Map<MessageModel>(groupChat);

            // get a list of user of this group
            var listOfGroupUserId = await _dbContext.GroupUser
                .Where(x => x.GroupId == groupChat.GroupId)
                .Select(x => x.UserId)
                .ToListAsync();

            // Call signalr to all user of this group to delete the message if available
            await _chatHub.Clients.Users(listOfGroupUserId).UpdateGroupMessage(groupChat.GroupId, messageModel);

            // return ok status
            return Ok();
        }

        [HttpPost("make-admin")]
        [Authorize]
        public async Task<IActionResult> MakeGroupAdmin(UpdateGroupUserModel model)
        {
            // Get the user own Id --------------------------------------------------------------------------
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // find the group
            var group = await _dbContext.Group
                .Include(x => x.GroupUsers)
                .SingleOrDefaultAsync(x => x.Id == model.GroupId);
            if (group == null) return NotFound(new { error = "No such group." });

            // check is this user is within this group or not ------------------------------------------------
            // and also check is this user has been deleted or not
            // and also check is this user is admin or not
            var groupUser = await _dbContext.GroupUser
                .Include(x => x.User)
                .SingleOrDefaultAsync(x =>
                    x.GroupId == model.GroupId
                    && x.UserId == userId
                    && x.OnDelete == null
                    && x.IsAdmin == true
                );
            if (groupUser == null) return NotFound(new { error = "Not allow to remove user as you are not an admin." });

            // check is model's userid exist in this group or not -------------------------------------------
            var modelUser = await _dbContext.GroupUser
                .Include(x => x.User)
                .SingleOrDefaultAsync(x =>
                    x.GroupId == model.GroupId
                    && x.UserId == model.UserId
                    && x.OnDelete == null
                );
            if (modelUser == null) return NotFound(new { error = "UserId not exist." });

            // Append group info ----------------------------------------------------------------------------
            var groupChat = new GroupChat
            {
                GroupId = model.GroupId,
                SenderId = null,
                Info = $"{groupUser.User.Name} made {modelUser.User.Name} as admin",
                OnCreate = DateTime.Now,
            };
            await _dbContext.GroupChat.AddAsync(groupChat);

            // turn modelUser isAdmin to true ---------------------------------------------------------------
            modelUser.IsAdmin = true;

            // This user will seen the message --------------------------------------------------------------
            var groupChatSeen = new GroupChatSeen
            {
                GroupChat = groupChat,
                UserId = userId,
                OnSeen = DateTime.Now,
            };
            await _dbContext.GroupChatSeen.AddAsync(groupChatSeen);

            // Save database changes
            await _dbContext.SaveChangesAsync();

            // map to messageModel DTO ----------------------------------------------------------------------
            var messageModel = _mapper.Map<MessageModel>(groupChat);

            // prepare a list of group user id that exist in this group
            var listOfGroupUserId = group.GroupUsers
                .Where(x => x.OnDelete == null)
                .Select(x => x.UserId);

            // send message to these users of the group
            await _chatHub.Clients.Users(listOfGroupUserId).AppendGroupMessage(group.Id, messageModel);

            // prepare a notification message --------------------------------------------------------------
            var payload = new NotificationPayloadModel
            {
                SenderName = group.Name,
                GroupId = group.Id,
                Message = messageModel.Info,
            };

            // convert payload to json
            var payloadJSON = JsonConvert.SerializeObject(payload);

            // get the list of user (except this user) exist in this group
            // loop through them and send the notification
            var otherListOfGroupUserId = listOfGroupUserId.Where(x => x != userId);
            foreach (var id in otherListOfGroupUserId)
            {
                await _notificationService.SendNotificationAsync(id, payloadJSON);
            }

            // increment notification to users (except this user) ------------------------------------------
            await _chatHub.Clients.Users(otherListOfGroupUserId).IncrementGroupNotification(group.Id);

            return Ok();
        }

        [HttpPost("dismiss-admin")]
        [Authorize]
        public async Task<IActionResult> DismissGroupAdmin(UpdateGroupUserModel model)
        {
            // Get the user own Id --------------------------------------------------------------------------
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // find the group
            var group = await _dbContext.Group
                .Include(x => x.GroupUsers)
                .SingleOrDefaultAsync(x => x.Id == model.GroupId);
            if (group == null) return NotFound(new { error = "No such group." });

            // check is this user is within this group or not ------------------------------------------------
            // and also check is this user has been deleted or not
            // and also check is this user is admin or not
            var groupUser = await _dbContext.GroupUser
                .Include(x => x.User)
                .SingleOrDefaultAsync(x =>
                    x.GroupId == model.GroupId
                    && x.UserId == userId
                    && x.OnDelete == null
                    && x.IsAdmin == true
                );
            if (groupUser == null) return NotFound(new { error = "Not allow to remove user as you are not an admin." });

            // check is model's userid exist in this group or not -------------------------------------------
            var modelUser = await _dbContext.GroupUser
                .Include(x => x.User)
                .SingleOrDefaultAsync(x =>
                    x.GroupId == model.GroupId
                    && x.UserId == model.UserId
                    && x.OnDelete == null
                );
            if (modelUser == null) return NotFound(new { error = "UserId not exist." });

            // Append group info ----------------------------------------------------------------------------
            var groupChat = new GroupChat
            {
                GroupId = model.GroupId,
                SenderId = null,
                Info = $"{groupUser.User.Name} made {modelUser.User.Name} as member",
                OnCreate = DateTime.Now,
            };
            await _dbContext.GroupChat.AddAsync(groupChat);

            // turn modelUser isAdmin to false --------------------------------------------------------------
            modelUser.IsAdmin = false;

            // This user will seen the message --------------------------------------------------------------
            var groupChatSeen = new GroupChatSeen
            {
                GroupChat = groupChat,
                UserId = userId,
                OnSeen = DateTime.Now,
            };
            await _dbContext.GroupChatSeen.AddAsync(groupChatSeen);

            // Save database changes
            await _dbContext.SaveChangesAsync();

            // map to messageModel DTO ----------------------------------------------------------------------
            var messageModel = _mapper.Map<MessageModel>(groupChat);

            // prepare a list of group user id that exist in this group
            var listOfGroupUserId = group.GroupUsers
                .Where(x => x.OnDelete == null)
                .Select(x => x.UserId);

            // send message to these users of the group
            await _chatHub.Clients.Users(listOfGroupUserId).AppendGroupMessage(group.Id, messageModel);

            // prepare a notification message --------------------------------------------------------------
            var payload = new NotificationPayloadModel
            {
                SenderName = group.Name,
                GroupId = group.Id,
                Message = messageModel.Info,
            };

            // convert payload to json
            var payloadJSON = JsonConvert.SerializeObject(payload);

            // get the list of user (except this user) exist in this group
            // loop through them and send the notification
            var otherListOfGroupUserId = listOfGroupUserId.Where(x => x != userId);
            foreach (var id in otherListOfGroupUserId)
            {
                await _notificationService.SendNotificationAsync(id, payloadJSON);
            }

            // increment notification to users (except this user) ------------------------------------------
            await _chatHub.Clients.Users(otherListOfGroupUserId).IncrementGroupNotification(group.Id);

            return Ok();
        }

        [HttpGet("info/{groupId}")]
        [Authorize]
        public async Task<IActionResult> GetGroupInfo([FromRoute] int groupId)
        {
            // get the group -------------------------------------------------------------------------------------------
            var group = await _dbContext.Group
                .Include(x => x.GroupUsers)
                    .ThenInclude(x => x.User)
                .SingleOrDefaultAsync(x => x.Id == groupId);
            if (group == null) return NotFound(new { error = "No such group." });

            // Get the user own Id -------------------------------------------------------------------------------------
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // check is this user is within this group or not
            var thisGroupUser = await _dbContext.GroupUser
                .SingleOrDefaultAsync(x => 
                    x.GroupId == groupId 
                    && x.UserId == userId
                    && x.OnDelete == null
                );
            if (thisGroupUser == null) return StatusCode(StatusCodes.Status405MethodNotAllowed, new { error = "you are no longer a member in this group." });

            // prepare groupInfoModel
            var model = new GroupInfoModel
            {
                IsAdmin = thisGroupUser.IsAdmin,
                Users = group.GroupUsers.Select(x => new GroupUserModel
                {
                    Id = x.User.Id,
                    OnDelete = x.OnDelete,
                    IsAdmin = x.IsAdmin,
                })
                .ToList(),
            };

            // return model
            return Ok(model);
        }

        [HttpGet("message")]
        [Authorize]
        public async Task<IActionResult> GetGroupMessage([FromQuery] int groupId, [FromQuery] int messageId)
        {
            // Get the user own Id --------------------------------------------------------------------------
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // find the group
            var group = await _dbContext.Group
                .AsNoTracking()
                .Include(x => x.GroupUsers)
                .SingleOrDefaultAsync(x => x.Id == groupId);
            if (group == null) return NotFound(new { error = "No such group." });

            // check is this user is within this group or not ------------------------------------------------
            var groupUser = await _dbContext.GroupUser
                .AsNoTracking()
                .Include(x => x.User)
                .SingleOrDefaultAsync(x =>
                    x.GroupId == groupId
                    && x.UserId == userId
                );
            if (groupUser == null) return NotFound(new { error = "Not allow to view this message info." });

            // check is the group chat exist or not ---------------------------------------------------------
            // and also check is the user is exited after the group chat is send.
            // and also check is info null or not
            var groupChat = await _dbContext.GroupChat
                .AsNoTracking()
                .Include(x => x.GroupChatSeens)
                .SingleOrDefaultAsync(x =>
                    x.GroupId == groupId
                    && x.Id == messageId
                    && ((groupUser.OnDelete == null) || (x.OnCreate <= groupUser.OnDelete))
                    && x.Info == null
                );
            if (groupChat == null) return NotFound(new { error = "Group chat not found." });

            // get list of InfoGroupMessageSeenModel
            var listOfGroupUser = group.GroupUsers
                    .Where(x => x.OnDelete == null || groupChat.OnCreate <= x.OnDelete)
                    .Select(x => new InfoGroupMessageSeenModel
                    {
                        UserId = x.UserId,
                        OnSeen = groupChat.GroupChatSeens.SingleOrDefault(y => y.UserId == x.UserId)?.OnSeen,
                    })
                    .ToList();

            // prepare the mssage model
            var messageModel = _mapper.Map<MessageModel>(groupChat);

            // get the list of onseen count and compare to the group of user that is not exited yet
            // if onSeenCount is equal and larger to groupUser, then its means the message was seen
            var OnSeenCount = groupChat.GroupChatSeens.Count();
            messageModel.OnSeen = (OnSeenCount >= group.GroupUsers.Count(x => x.OnDelete == null));

            // prepare the InfoGroupMessageModel -------------------------------------------------------------
            var model = new InfoGroupMessageModel
            {
                Message = _mapper.Map<MessageModel>(groupChat),
                SeenBy = listOfGroupUser,
            };

            return Ok(model);
        }

        [HttpGet("image/{groupId}")]
        [Authorize]
        public async Task<IActionResult> GetImage([FromRoute] int groupId)
        {
            // get the group
            var group = await _dbContext.Group.FindAsync(groupId);
            if (group == null) return NotFound(new { error = "No such group." });

            // Get the user own Id
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // check is this user is within this group or not
            var isExist = await _dbContext.GroupUser
                .SingleOrDefaultAsync(x => 
                    x.GroupId == groupId 
                    && x.UserId == userId
                );
            if (isExist == null) return StatusCode(StatusCodes.Status405MethodNotAllowed, new { error = "Not allow to view this image." });

            // return file
            return File(group.File, group.ContentType);
        }

        [HttpGet("file/{messageId:int}")]
        [Authorize]
        public async Task<IActionResult> GetFile([FromRoute] int messageId)
        {
            // Get the user own Id
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // find the group chat
            var groupChat = await _dbContext.GroupChat.FindAsync(messageId);
            if (groupChat == null) return NotFound(new { error = "No such group chat." });

            // check is this user is within this group or not
            var groupUser = await _dbContext.GroupUser
                .AsNoTracking()
                .SingleOrDefaultAsync(x => 
                    x.GroupId == groupChat.GroupId 
                    && x.UserId == userId
                );
            if (groupUser == null) return NotFound(new { error = "Not allow to view this file." });

            // check if the group user is exited the group or not
            // if is exited, check the file datetime to determine whether the user can view the file or not
            if (groupUser.OnDelete != null && groupChat.OnCreate > groupUser.OnDelete)
            {
                return NotFound(new { error = "Not allow to view this file." });
            }

            // if there is no file, do nothing
            if (groupChat.File == null) return NotFound(new { error = "No such file." });

            // return file
            return File(groupChat.File, groupChat.ContentType);
        }
    }
}
