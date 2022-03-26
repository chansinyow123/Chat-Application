using AutoMapper;
using backend.Data;
using backend.Models.Account;
using backend.Models.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Hubs
{
    [Authorize]
    public class ChatHub : Hub<IChatHub>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public ChatHub(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _mapper = mapper;
        }

        public override async Task OnConnectedAsync()
        {
            // Increment status online for this account -------------------------------------------------------------
            var caller = await _userManager.FindByIdAsync(Context.UserIdentifier);
            caller.Online++;
            await _userManager.UpdateAsync(caller);

            // Call to all clients to update account online status
            var userModel = _mapper.Map<UserModel>(caller);
            await Clients.All.UpdateAccount(userModel);

            await base.OnConnectedAsync();
        }

        public async Task<List<UserModel>> GetContact()
        {
            // load account information --------------------------------------------------------------------------
            var users = await _userManager.Users.AsNoTracking().ToListAsync();
            var accounts = _mapper.Map<List<UserModel>>(users);

            return accounts;
        }

        public async Task<List<GroupModel>> GetGroupInfo()
        {
            // get a list of group that associate with this user id -----------------------------------------------
            var thisUserGroup = await _dbContext.GroupUser
                .AsNoTracking()
                .Where(x => x.UserId == Context.UserIdentifier)
                .ToListAsync();

            // prepare group model
            var model = new List<GroupModel>();

            foreach (var groupUser in thisUserGroup)
            {

                // query group info
                var group = await _dbContext.Group
                    .AsNoTracking()
                    .Include(x => x.GroupUsers)
                    .SingleOrDefaultAsync(x => x.Id == groupUser.GroupId);

                // add to groupModel
                var groupModel = _mapper.Map<GroupModel>(group);
                groupModel.IsExit = (groupUser.OnDelete != null);

                model.Add(groupModel);
            }

            // return list of group model
            return model;
        }

        public async Task<List<RecentChatModel>> GetRecentChat()
        {
            // load account information --------------------------------------------------------------------------
            var users = await _userManager.Users.AsNoTracking().ToListAsync();
            var accounts = _mapper.Map<List<UserModel>>(users);

            // loop through user to get recentChat ---------------------------------------------------------------
            var recentChat = new List<RecentChatModel>();

            // this is to know how many message should be retrieve from the database
            var take = 1;

            foreach (var user in accounts)
            {
                // take only 1 chat with specified condition
                // the OrderByDescending is used when we are querying
                // the OrderBy is used for display purpose
                var privateChat = await _dbContext.PrivateChat
                    .AsNoTracking()
                    .Where(p =>
                        (p.SenderId == user.Id && p.ReceiverId == Context.UserIdentifier) ||
                        (p.ReceiverId == user.Id && p.SenderId == Context.UserIdentifier)
                    )
                    .OrderByDescending(p => p.OnCreate)
                    .Take(take)
                    .OrderBy(p => p.OnCreate)
                    .ToListAsync();

                // if there are no private chat, then continue loop
                if (privateChat.Count <= 0) continue;

                // map to list of messageModel
                var privateMessages = _mapper.Map<List<MessageModel>>(privateChat);

                // get the total notification count
                var notificationCount = await _dbContext.PrivateChat
                    .AsNoTracking()
                    .CountAsync(p =>
                        p.SenderId == user.Id &&
                        p.ReceiverId == Context.UserIdentifier &&
                        p.OnSeen == null
                    );

                // add to recentChatModel
                recentChat.Add(new RecentChatModel
                {
                    NotificationCount = notificationCount,
                    GroupId = null,
                    UserId = user.Id,
                    Messages = privateMessages
                });
            }

            // get a list of group that associate with this user id -----------------------------------------------
            var thisUserGroup = await _dbContext.GroupUser
                .AsNoTracking()
                .Where(x => x.UserId == Context.UserIdentifier)
                .ToListAsync();

            foreach (var groupUser in thisUserGroup)
            {
                // assign onDelete for later checking is the user exited the group.
                var onDelete = groupUser.OnDelete;

                // query group info
                var group = await _dbContext.Group
                    .AsNoTracking()
                    .Include(x => x.GroupUsers)
                    .SingleOrDefaultAsync(x => x.Id == groupUser.GroupId);

                // get group message that is available before exited the group.
                var groupChats = await _dbContext.GroupChat
                    .AsNoTracking()
                    .Where(x => 
                        ((onDelete == null) || (x.OnCreate <= onDelete))
                        && x.GroupId == groupUser.GroupId    
                    )
                    .OrderByDescending(p => p.OnCreate)
                    .Take(take)
                    .OrderBy(p => p.OnCreate)
                    .ToListAsync();

                // map to list of messageModel
                var groupMessages = _mapper.Map<List<MessageModel>>(groupChats);

                // foreach the groupMessage to assign onSeen
                foreach (var message in groupMessages)
                {
                    // get the list of onseen Count and compare to the group of user that is not exited yet
                    // if onSeenCount is equal and larger to groupUser, then its means the message was seen
                    var OnSeenCount = await _dbContext.GroupChatSeen.CountAsync(x => x.GroupChatId == message.Id);
                    message.OnSeen = (OnSeenCount >= group.GroupUsers.Count(x => x.OnDelete == null));
                }

                // get the total notification count -------------------------------------------------------------------
                var notificationCount = await _dbContext.GroupChat
                    .AsNoTracking()
                    .CountAsync(x =>
                        ((onDelete == null) || (x.OnCreate <= onDelete))
                        && x.GroupId == groupUser.GroupId
                        && !_dbContext.GroupChatSeen
                             .Where(y => y.UserId == Context.UserIdentifier)
                             .Select(y => y.GroupChatId)
                             .Contains(x.Id)
                    );

                // add to recentChatModel ------------------------------------------------------------------------------
                recentChat.Add(new RecentChatModel
                {
                    NotificationCount = notificationCount,
                    GroupId = group.Id,
                    UserId = null,
                    Messages = groupMessages
                });
            }

            // Order by decending OnCreate
            recentChat = recentChat
                .OrderByDescending(r => r.Messages.LastOrDefault().OnCreate)
                .ToList();

            // Load RecentChat for caller
            return recentChat;
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Decrement status online for this account ------------------------------------------------------
            var caller = await _userManager.FindByIdAsync(Context.UserIdentifier);
            caller.Online--;
            await _userManager.UpdateAsync(caller);

            // Call to all clients to update account online status
            var userModel = _mapper.Map<UserModel>(caller);
            await Clients.All.UpdateAccount(userModel);

            await base.OnDisconnectedAsync(exception);
        }
    }
}
