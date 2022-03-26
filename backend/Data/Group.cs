using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Data
{
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] File { get; set; }
        public string ContentType { get; set; }

        public List<GroupUser> GroupUsers { get; set; }

        public List<GroupChat> GroupChats { get; set; }
    }
}
