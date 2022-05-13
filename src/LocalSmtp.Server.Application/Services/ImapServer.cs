/*
Copyright(c) 2009 - 2018, smtp4dev project contributors All rights reserved.
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
    * Neither the name of smtp4dev nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS;
OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/Server
*/

using LocalSmtp.Server.Application.Extensions;
using LocalSmtp.Server.Application.Repositories.Abstractions;
using LocalSmtp.Server.Infrastructure.Data;
using LocalSmtp.Server.Infrastructure.Models;
using LumiSoft.Net;
using LumiSoft.Net.IMAP;
using LumiSoft.Net.IMAP.Server;
using LumiSoft.Net.Mail;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Reactive.Linq;

namespace LocalSmtp.Server.Application.Services;

public class ImapServer
{
    public ImapServer(IServiceScopeFactory serviceScopeFactory, IOptionsMonitor<ServerOptions> serverOptions, ILogger<ImapServer> logger)
    {
        this.serverOptions = serverOptions;
        _logger = logger;
        this.serviceScopeFactory = serviceScopeFactory;

        IDisposable eventHandler = null;
        var obs = Observable.FromEvent<ServerOptions>(e => eventHandler = serverOptions.OnChange(e), e => eventHandler.Dispose());
        obs.Throttle(TimeSpan.FromMilliseconds(100)).Subscribe(OnServerOptionsChanged);

        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<AppDbContext>();
        if (!dbContext.ImapState.Any())
        {
            dbContext.Add(new ImapState
            {
                Id = Guid.Empty,
                LastUid = 1
            });
            dbContext.SaveChanges();
        }
    }

    private void OnServerOptionsChanged(ServerOptions serverOptions)
    {
        Stop();

        TryStart();
    }

    public bool IsRunning
    {
        get
        {
            return imapServer?.IsRunning ?? false;
        }
    }

    public void TryStart()
    {
        if (!serverOptions.CurrentValue.ImapPort.HasValue)
        {
            _logger.LogInformation("IMAP server disabled");
            return;
        }

        imapServer = new IMAP_Server()
        {
            Bindings = new[] { new IPBindInfo(Dns.GetHostName(), BindInfoProtocol.TCP, serverOptions.CurrentValue.AllowRemoteConnections ? IPAddress.Any : IPAddress.Loopback, serverOptions.CurrentValue.ImapPort.Value) },
            GreetingText = "LocalSmtp"
        };
        imapServer.SessionCreated += (o, args) => new SessionHandler(args.Session, this.serviceScopeFactory);


        TaskCompletionSource<Error_EventArgs>? errorTcs = new();
        imapServer.Error += (s, ea) =>
        {
            if (!errorTcs.Task.IsCompleted)
            {
                errorTcs.SetResult(ea);
            }
        };
        TaskCompletionSource<EventArgs>? startedTcs = new();
        imapServer.Started += (s, ea) => startedTcs.SetResult(ea);

        imapServer.Start();

        var errorTask = errorTcs.Task;
        var startedTask = startedTcs.Task;

        var index = Task.WaitAny(startedTask, errorTask, Task.Delay(TimeSpan.FromSeconds(30)));

        if (index == 1)
        {
            Console.WriteLine("The IMAP server failed to start: " + errorTask.Result.Exception.ToString());
        }
        else if (index == 2)
        {
            Console.WriteLine("The IMAP server failed to start: Timeout");

            try
            {
                imapServer.Stop();
            }
            catch { }
        }
        else
        {
            var port = ((IPEndPoint)imapServer.ListeningPoints[0].Socket.LocalEndPoint).Port;
            _logger.LogInformation("IMAP Server is listening on port {port}", port);
        }
    }

    public void Stop()
    {
        imapServer?.Stop();
        imapServer = null;
    }

    private IMAP_Server imapServer;
    private readonly IOptionsMonitor<ServerOptions> serverOptions;
    private readonly ILogger<ImapServer> _logger;
    private readonly IServiceScopeFactory serviceScopeFactory;

    private void Logger_WriteLog(object sender, LumiSoft.Net.Log.WriteLogEventArgs e)
    {
        _logger.LogInformation(e.LogEntry.Text);
    }

    class SessionHandler
    {
        public SessionHandler(IMAP_Session session, IServiceScopeFactory serviceScopeFactory)
        {
            this.session = session;
            session.List += Session_List;
            session.Login += Session_Login;
            session.Fetch += Session_Fetch;
            session.GetMessagesInfo += Session_GetMessagesInfo;
            session.Capabilities.Remove("QUOTA");
            session.Capabilities.Remove("NAMESPACE");
            session.Store += Session_Store;
            session.Select += Session_Select;
            this.serviceScopeFactory = serviceScopeFactory;
        }


        private void Session_Select(object sender, IMAP_e_Select e)
        {
            e.Flags.Clear();
            e.Flags.Add("\\Deleted");
            e.Flags.Add("\\Seen");

            e.PermanentFlags.Clear();
            e.PermanentFlags.Add("\\Deleted");
            e.PermanentFlags.Add("\\Seen");

            e.FolderUID = 1234;
        }

        private void Session_Store(object sender, IMAP_e_Store e)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();

            if (e.FlagsSetType == IMAP_Flags_SetType.Add || e.FlagsSetType == IMAP_Flags_SetType.Replace)
            {
                if (e.Flags.Contains("Seen", StringComparer.OrdinalIgnoreCase))
                {
                    messagesRepository.MarkMessageRead(new Guid(e.MessageInfo.ID));
                }

                if (e.Flags.Contains("Deleted", StringComparer.OrdinalIgnoreCase))
                {
                    messagesRepository.DeleteMessage(new Guid(e.MessageInfo.ID));
                }
            }
        }

        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly IMAP_Session session;

        private void Session_GetMessagesInfo(object sender, IMAP_e_MessagesInfo e)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();

            if (e.Folder == "INBOX")
            {
                foreach (var message in messagesRepository.GetMessages())
                {
                    List<string> flags = new();
                    if (!message.IsUnread)
                    {
                        flags.Add("Seen");
                    }

                    e.MessagesInfo.Add(new IMAP_MessageInfo(message.Id.ToString(), message.ImapUid, flags.ToArray(), message.Data.Length, message.ReceivedDate));
                }
            }
        }

        private void Session_Fetch(object sender, IMAP_e_Fetch e)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var messagesRepository = scope.ServiceProvider.GetService<IMessagesRepository>();

            foreach (var msgInfo in e.MessagesInfo)
            {
                var dbMessage = messagesRepository.GetMessages().SingleOrDefault(m => m.Id == new Guid(msgInfo.ID));

                if (dbMessage != null)
                {
                    var apiMessage = dbMessage.ToApiModel();
                    var message = Mail_Message.ParseFromByte(apiMessage.Data);
                    e.AddData(msgInfo, message);
                }
            }

        }

        private void Session_Login(object sender, IMAP_e_Login e)
        {
            e.IsAuthenticated = true;
        }

        private void Session_List(object sender, IMAP_e_List e)
        {
            e.Folders.Add(new IMAP_r_u_List("INBOX", '/', new string[0]));

        }
    }
}
