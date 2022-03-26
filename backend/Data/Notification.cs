using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Data
{
    public class Notification
    {
        public int Id { get; set; }
        public string Endpoint { get; set; }
        public string P256dh { get; set; }
        public string Auth { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
