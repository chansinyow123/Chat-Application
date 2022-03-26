using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Chat
{
    public class LoadMessageModel
    {
        public bool HaveMoreChat { get; set; }
        public List<MessageModel> Messages { get; set; }
    }
}
