using backend.Data;
using backend.Models.Account;
using backend.Models.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Hubs
{
    public interface IChatHub
    {
        Task AddAccount(UserModel account);
        Task UpdateAccount(UserModel account);
        Task AppendPrivateMessage(string userId, MessageModel message);
        Task IncrementPrivateNotification(string receiverId);
        Task PrivateSeen(string receiverId);
        Task UpdatePrivateMessage(string receiverId, MessageModel message);
        Task CreateGroupChat(GroupModel group);
        Task GroupSeen(int groupId);
        Task AppendGroupMessage(int groupId, MessageModel message);
        Task ClearGroupMessage(int groupId);
        Task IncrementGroupNotification(int groupId);
        Task UpdateGroupMessage(int groupId, MessageModel message);
        Task UpdateGroup(GroupModel group);
    }
}
