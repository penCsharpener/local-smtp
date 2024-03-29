﻿/*
Copyright(c) 2009 - 2018, smtp4dev project contributors All rights reserved.
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
    * Neither the name of smtp4dev nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS;
OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/Hubs
*/

using Microsoft.AspNetCore.SignalR;

namespace LocalSmtp.Server.Application.Hubs;

public class NotificationsHub : Hub
{
    private readonly IHubContext<NotificationsHub> _context;

    public NotificationsHub(IHubContext<NotificationsHub> context)
    {
        _context = context;
    }

    public async Task OnMessagesChanged()
    {
        if (_context.Clients != null)
        {
            await _context.Clients.All.SendAsync("messageschanged", "msg");
        }
    }

    public async Task OnServerChanged()
    {
        if (_context.Clients != null)
        {
            await _context.Clients.All.SendAsync("serverchanged", "msg");
        }
    }

    public async Task OnSessionsChanged()
    {
        if (_context.Clients != null)
        {
            await _context.Clients.All.SendAsync("sessionschanged", "msg");
        }
    }

    public async Task OnMessageReadChanged(Guid id)
    {
        if (_context.Clients != null)
        {
            await _context.Clients.All.SendAsync("messageReadChanged", id);
        }
    }
}
