using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class MessageEntity
    {
        [Key] public int Id { get; set; }
        public required string Text { get; set; }
        public int SenderId { get; set; }
        public int RecepientId { get; set; }
        public DateTime CreateAt { get; set; }
    }
}
