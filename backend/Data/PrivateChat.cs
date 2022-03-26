using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Data
{
    public class PrivateChat
    {
        public int Id { get; set; }
        public string Info { get; set; }
        public string Message { get; set; }
        public string Location { get; set; }
        public byte[] File { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
        public DateTime OnCreate { get; set; }
        public DateTime? OnDelete { get; set; }
        public DateTime? OnSeen { get; set; }

        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }
        public string ReceiverId { get; set; }
        public ApplicationUser Receiver { get; set; }

    }
}
