using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Chat
{
    public class NotificationPayloadModel
    {
        public string SenderName { get; set; }
        public string PrivateId { get; set; }
        public int GroupId { get; set; }
        public string Message { get; set; }
    }
}
