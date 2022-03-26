using backend.Validation;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Group
{
    public class CreateGroupModel
    {
        [RequiredList]
        public List<string> UsersId { get; set; }
        public IFormFile File { get; set; }
        [Required]
        [MaxLength(30)]
        public string GroupName { get; set; }
    }
}
