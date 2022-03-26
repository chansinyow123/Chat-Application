using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Group
{
    public class UpdateGroupUserModel
    {
        public int GroupId { get; set; }
        public string UserId { get; set; }
    }
}
