// source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/Server

namespace LocalSmtp.Server.Application.Services;

public class RelayRecipientResult
{
    public string Email { get; set; }
    public DateTime RelayDate { get; set; }
}
