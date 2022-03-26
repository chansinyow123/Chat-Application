using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Group
{
    public class GroupUserModel
    {
        public string Id { get; set; }
        public DateTime? OnDelete { get; set; }
        public bool IsAdmin { get; set; }
    }
}
