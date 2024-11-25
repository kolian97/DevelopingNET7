using Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace App.Contract
{
    public class Message
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public int SenderID { get; set; }
        public int? RecepentId { get; set; } = -1;
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public Command Command { get; set; } = Command.None;
        public IEnumerable<User> Users { get; set; } = [];
        public static Message FromDomain(MessageEntity entity)
        {
            return new Message
            {
                Id = entity.Id,
                SenderID = entity.SenderId,
                RecepentId = entity.RecepientId,
                CreateAt = entity.CreateAt
            };
        }
    }
    public enum Command
    {
        Users,
        None,
        Join,
        Exit,
        Confirm
    }
    public class ReceivResult
    {
        public Message? Message { get; set; }
        public IPEndPoint? Endpoint { get; set; }
    }
}
