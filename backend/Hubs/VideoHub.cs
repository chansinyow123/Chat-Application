using AutoMapper;
using backend.Data;
using backend.Models.Account;
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
    public class VideoHub : Hub<IVideoHub>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<ChatHub, IChatHub> _chatHub;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public VideoHub(
            ApplicationDbContext dbContext, 
            IHubContext<ChatHub, IChatHub> chatHub,
            UserManager<ApplicationUser> userManager,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _chatHub = chatHub;
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
            await _chatHub.Clients.All.UpdateAccount(userModel);

            await base.OnConnectedAsync(); 
        }

        public async Task<bool?> CheckMeeting(string meetId)
        {
            // Remove all the meeting that is over 24 hour
            var expireMeet = await _dbContext.VideoCall
                .Where(v => v.OnCreate.AddHours(24) < DateTime.Now)
                .ToListAsync();

            // if there are expire meeting, remove it
            if (expireMeet.Count > 0)
            {
                _dbContext.VideoCall.RemoveRange(expireMeet);
                await _dbContext.SaveChangesAsync();
            }

            // check if the meetId is stored inside the database
            // if not, return false
            var meet = await _dbContext.VideoCall.FindAsync(meetId);
            if (meet == null) return null;

            // if meet member is larger than 1, then return false
            if (meet.Member > 1) return false;

            // else, add the member count
            meet.Member++;
            await _dbContext.SaveChangesAsync();

            // Add to Groups
            await Groups.AddToGroupAsync(Context.ConnectionId, meetId);

            // and store inside context.item to persist data within this connection
            Context.Items.Add("meetId", meetId);

            // return true
            return true;
        }

        public async Task SendPeerId(string peerId)
        {
            // get the meetId from Items, if no meetId, do nothing
            Context.Items.TryGetValue("meetId", out var value);
            if (value == null) return;

            // convert meetId to string
            var meetId = value as string;

            // send peer id to other people in the group
            await Clients.OthersInGroup(meetId).ReceivePeerId(peerId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // get the meetId from Items, if no meetId, do nothing -------------------------------------------
            Context.Items.TryGetValue("meetId", out var value);
            if (value == null) return;

            // convert meetId to string
            var meetId = value as string;

            // Remove from Groups
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, meetId);

            // decrement meet member
            var meet = await _dbContext.VideoCall.FindAsync(meetId);
            meet.Member--;

            // Save database changes
            await _dbContext.SaveChangesAsync();

            // Decrement status online for this account ------------------------------------------------------
            var caller = await _userManager.FindByIdAsync(Context.UserIdentifier);
            caller.Online--;
            await _userManager.UpdateAsync(caller);

            // Call to all clients to update account online status
            var userModel = _mapper.Map<UserModel>(caller);
            await _chatHub.Clients.All.UpdateAccount(userModel);

            await base.OnDisconnectedAsync(exception);
        }
    }
}
