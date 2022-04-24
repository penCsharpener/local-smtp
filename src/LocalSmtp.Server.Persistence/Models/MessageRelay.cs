// source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/DbModel
using System.ComponentModel.DataAnnotations;

namespace LocalSmtp.Server.Infrastructure.Models
{
    public class MessageRelay
    {
        [Key]
        public Guid Id { get; set; }
        public Guid MessageId { get; set; }
        public virtual Message Message { get; set; }
        public string To { get; set; }
        public DateTime SendDate { get; set; }
    }
}