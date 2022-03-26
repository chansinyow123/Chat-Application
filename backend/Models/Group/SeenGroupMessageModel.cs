using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Group
{
    public class SeenGroupMessageModel
    {
        [Required]
        public int GroupId { get; set; }
    }
}
