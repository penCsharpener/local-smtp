// source: https://github.com/rnwood/smtp4dev/blob/master/Rnwood.Smtp4dev/Data/

using LocalSmtp.Server.Infrastructure.Data;
using LocalSmtp.Server.Infrastructure.Models;

namespace LocalSmtp.Server.Repositories.Abstractions
{
    public interface IMessagesRepository
    {
        AppDbContext DbContext { get; }
        Task MarkMessageRead(Guid id);
        IQueryable<Message> GetMessages(bool unTracked = true);
        Task DeleteMessage(Guid id);
        Task DeleteAllMessages();
    }
}