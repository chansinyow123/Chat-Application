using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Chat
{
    public class GroupModel
    {
        public int Id { get; set; }
        public string Image { get; set; }
        public string Name { get; set; }
        public bool IsExit { get; set; }
    }
}
