using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Data
{
    public class GroupUser
    {
        public int GroupId { get; set; }
        public Group Group { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public bool IsAdmin { get; set; }
        public DateTime? OnDelete { get; set; }
    }
}
