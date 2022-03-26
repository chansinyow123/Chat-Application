using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Data
{
    public class GroupChatSeen
    {
        public int GroupChatId { get; set; }
        public GroupChat GroupChat { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public DateTime OnSeen { get; set; }
    }
}
