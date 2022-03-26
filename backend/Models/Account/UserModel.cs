using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Account
{
    public class UserModel
    {
        public string Id { get; set; }
        public string Image { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public int Online { get; set; }
        public bool NeedNotification { get; set; }
    }
}
