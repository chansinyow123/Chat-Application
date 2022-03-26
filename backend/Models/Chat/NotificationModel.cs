using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Chat
{
    public class NotificationModel
    {
        [Required]
        public string Endpoint { get; set; }
        [Required]
        public string P256dh { get; set; }
        [Required]
        public string Auth { get; set; }
    }
}
