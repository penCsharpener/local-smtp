// source: https://github.com/rnwood/smtp4dev/blob/master/Rnwood.Smtp4dev/Data/
using LocalSmtp.Server.Infrastructure.Data;
using LocalSmtp.Server.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace LocalSmtp.Server.Repositories
{
    public class MessagesRepository : IMessagesRepository
    {
        private readonly ITaskQueue taskQueue;
        private readonly NotificationsHub notificationsHub;
        private readonly AppDbContext dbContext;

        public MessagesRepository(ITaskQueue taskQueue, NotificationsHub notificationsHub, AppDbContext dbContext)
        {
            this.taskQueue = taskQueue;
            this.notificationsHub = notificationsHub;
            this.dbContext = dbContext;
        }

        public AppDbContext DbContext => dbContext;

        public Task MarkMessageRead(Guid id)
        {
            return taskQueue.QueueTask(() =>
            {
                var message = dbContext.Messages.FindAsync(id).Result;
                if (message?.IsUnread != true)
                    return;
                message.IsUnread = false;
                dbContext.SaveChanges();
                notificationsHub.OnMessagesChanged().Wait();
            }, true);
        }

        public IQueryable<Message> GetMessages(bool unTracked = true)
        {
            return unTracked ? dbContext.Messages.AsNoTracking() : dbContext.Messages;
        }

        public Task DeleteMessage(Guid id)
        {
            return taskQueue.QueueTask(() =>
            {
                dbContext.Messages.RemoveRange(dbContext.Messages.Where(m => m.Id == id));
                dbContext.SaveChanges();
                notificationsHub.OnMessagesChanged().Wait();
            }, true);
        }

        public Task DeleteAllMessages()
        {
            return taskQueue.QueueTask(() =>
            {
                dbContext.Messages.RemoveRange(dbContext.Messages);
                dbContext.SaveChanges();
                notificationsHub.OnMessagesChanged().Wait();
            }, true);
        }
    }
}