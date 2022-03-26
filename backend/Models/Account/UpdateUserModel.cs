using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Account
{
    public class UpdateUserModel
    {
        [Required]
        [MaxLength(30)]
        public string Name { get; set; }
    }
}
