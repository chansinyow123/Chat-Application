using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Group
{
    public class SendGroupMessageModel
    {
        [Required]
        public int GroupId { get; set; }
        [MaxLength(4096)]
        public string Message { get; set; }
        public IFormFile File { get; set; }
        public string Location { get; set; }
    }
}
