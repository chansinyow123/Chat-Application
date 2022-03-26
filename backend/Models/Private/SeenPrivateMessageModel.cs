using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Private
{
    public class SeenPrivateMessageModel
    {
        [Required]
        public string SenderId { get; set; }
    }
}
