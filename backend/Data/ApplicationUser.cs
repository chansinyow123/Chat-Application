using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace backend.Data 
{
	public class ApplicationUser : IdentityUser
    {
		public string Name { get; set; }
        public byte[] File { get; set; }
        public string ContentType { get; set; }
        public int Online { get; set; }
        public bool NeedNotification { get; set; }

        public List<Notification> Notifications { get; set; }

        public List<PrivateChat> SentPrivateChats { get; set; }
        public List<PrivateChat> ReceivedPrivateChats { get; set; }

        public List<GroupUser> GroupUsers { get; set; }

        public List<GroupChat> GroupChats { get; set; }

        public List<GroupChatSeen> GroupChatSeens { get; set; }
    }
}