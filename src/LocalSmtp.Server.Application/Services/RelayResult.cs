// source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/Server

using LocalSmtp.Server.Infrastructure.Models;
using MimeKit;

namespace LocalSmtp.Server.Application.Services;

public class RelayResult
{
    public RelayResult(Message message)
    {
        Message = message;
    }

    public Dictionary<MailboxAddress, Exception> Exceptions { get; set; } = new Dictionary<MailboxAddress, Exception>();
    public List<RelayRecipientResult> RelayRecipients { get; set; } = new List<RelayRecipientResult>();
    public bool WasRelayed => RelayRecipients.Count > 0;
    public Message Message { get; set; }
}