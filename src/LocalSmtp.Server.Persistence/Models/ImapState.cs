// source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/DbModel
using System.ComponentModel.DataAnnotations;

namespace LocalSmtp.Server.Infrastructure.Models
{
    public class ImapState
    {
        [Key]
        public Guid Id { get; set; }
        public long LastUid { get; set; }
    }
}
