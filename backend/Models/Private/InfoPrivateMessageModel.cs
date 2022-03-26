using backend.Models.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Private
{
    public class InfoPrivateMessageModel
    {
        public MessageModel Message { get; set; }
        public DateTime? OnSeen { get; set; }
    }
}
