using backend.Models.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Group
{
    public class GroupInfoModel
    {
        public bool IsAdmin { get; set; }
        public List<GroupUserModel> Users { get; set; }
    }
}
