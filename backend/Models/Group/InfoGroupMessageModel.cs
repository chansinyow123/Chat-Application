using backend.Models.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Group
{
    public class InfoGroupMessageModel
    {
        public MessageModel Message { get; set; }
        public List<InfoGroupMessageSeenModel> SeenBy { get; set; }
    }
}
