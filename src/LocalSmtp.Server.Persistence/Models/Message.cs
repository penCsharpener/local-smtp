// source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/DbModel
using System.ComponentModel.DataAnnotations;

namespace LocalSmtp.Server.Infrastructure.Models
{
    public class Message
    {
        [Key]
        public Guid Id { get; set; }
        public long ImapUid { get; internal set; }
        public string From { get; set; }
        public string To { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string Subject { get; set; }
        public byte[] Data { get; set; }
        public string MimeParseError { get; set; }
        public Session Session { get; set; }
        public int AttachmentCount { get; set; }
        public bool IsUnread { get; set; }
        public string RelayError { get; internal set; }
        public bool SecureConnection { get; set; }
        public virtual List<MessageRelay> Relays { get; set; } = new List<MessageRelay>();

        public void AddRelay(MessageRelay messageRelay)
        {
            messageRelay.Message = this;
            Relays.Add(messageRelay);
        }
    }
}