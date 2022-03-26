using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Group
{
    public class InfoGroupMessageSeenModel
    {
        public string UserId { get; set; }
        public DateTime? OnSeen { get; set; }
    }
}
