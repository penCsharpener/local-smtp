using LocalSmtp.Shared.ApiModels;
using MimeKit;

namespace LocalSmtp.Server.Application.Models;

public class ExtendedMessageEntitySummary : MessageEntitySummary
{
    public List<ExtendedMessageEntitySummary> ExtendedChildParts { get; set; }
    public MimeEntity MimeEntity { get; set; }
}
