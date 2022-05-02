/*
Copyright(c) 2009 - 2018, smtp4dev project contributors All rights reserved.
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
    * Neither the name of smtp4dev nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS;
OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/Data
*/

using LocalSmtp.Server.Application.Hubs;
using LocalSmtp.Server.Application.Repositories.Abstractions;
using LocalSmtp.Server.Application.Services.Abstractions;
using LocalSmtp.Server.Infrastructure.Data;
using LocalSmtp.Server.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalSmtp.Server.Application.Repositories;

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
            {
                return;
            }

            message.IsUnread = false;

            dbContext.SaveChanges();

            notificationsHub.OnMessageReadChanged(message.Id).Wait();
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