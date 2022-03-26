using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Account
{
    public class UploadImageModel
    {
        public IFormFile File { get; set; }
    }
}
