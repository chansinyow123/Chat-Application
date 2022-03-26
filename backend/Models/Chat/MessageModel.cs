using backend.Models.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Chat
{
    public class MessageModel
    {
        public int Id { get; set; }
        public string SenderId { get; set; }
        public string Info { get; set; }
        public string Message { get; set; }
        public string Location { get; set; }
        public string File { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
        public int? FileSize { get; set; }
        public DateTime OnCreate { get; set; }
        public DateTime? OnDelete { get; set; }
        public bool OnSeen { get; set; }
    }
}
