using backend.Models.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Chat
{
    public class RecentChatModel
    {
        public int NotificationCount { get; set; }
        public int? GroupId { get; set; }
        public string UserId { get; set; }
        
        public List<MessageModel> Messages { get; set; }
    }
}
