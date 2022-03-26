using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Group
{
    public class AddGroupUserModel
    {
        public int GroupId { get; set; }
        public List<string> UserIds { get; set; }
    }
}
