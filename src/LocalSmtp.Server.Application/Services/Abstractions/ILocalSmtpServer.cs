// source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/Server

using LocalSmtp.Server.Infrastructure.Models;
using MimeKit;

namespace LocalSmtp.Server.Application.Services.Abstractions;

public interface ILocalSmtpServer
{
    RelayResult TryRelayMessage(Message message, MailboxAddress[] overrideRecipients);
    Exception Exception { get; }
    bool IsRunning { get; }
    int PortNumber { get; }
    void TryStart();
    void Stop();
    Task DeleteSession(Guid id);
    Task DeleteAllSessions();
}