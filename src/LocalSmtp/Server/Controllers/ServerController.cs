/*
Copyright(c) 2009 - 2018, smtp4dev project contributors All rights reserved.
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
    * Neither the name of smtp4dev nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS;
OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/Controllers
*/

using LocalSmtp.Server.Application.Services;
using LocalSmtp.Server.Application.Services.Abstractions;
using LocalSmtp.Shared.ApiModels;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServerController : Controller
    {
        public ServerController(ILocalSmtpServer server, ImapServer imapServer, IOptionsMonitor<ServerOptions> serverOptions,
            IOptionsMonitor<RelayOptions> relayOptions, IHostingEnvironmentHelper hostingEnvironmentHelper)
        {
            this.server = server;
            this.imapServer = imapServer;
            this.serverOptions = serverOptions;
            this.relayOptions = relayOptions;
            this.hostingEnvironmentHelper = hostingEnvironmentHelper;
        }


        private ILocalSmtpServer server;
        private ImapServer imapServer;
        private IOptionsMonitor<ServerOptions> serverOptions;
        private IOptionsMonitor<RelayOptions> relayOptions;
        private readonly IHostingEnvironmentHelper hostingEnvironmentHelper;

        [HttpGet]
        public Server GetServer()
        {
            return new Server()
            {
                IsRunning = server.IsRunning,
                PortNumber = serverOptions.CurrentValue.Port,
                ImapPortNumber = serverOptions.CurrentValue.ImapPort,
                HostName = serverOptions.CurrentValue.HostName,
                AllowRemoteConnections = serverOptions.CurrentValue.AllowRemoteConnections,
                NumberOfMessagesToKeep = serverOptions.CurrentValue.NumberOfMessagesToKeep,
                NumberOfSessionsToKeep = serverOptions.CurrentValue.NumberOfSessionsToKeep,
                Exception = server.Exception?.Message,
                RelayOptions = new ServerRelayOptions
                {
                    SmtpServer = relayOptions.CurrentValue.SmtpServer,
                    TlsMode = relayOptions.CurrentValue.TlsMode.ToString(),
                    SmtpPort = relayOptions.CurrentValue.SmtpPort,
                    Login = relayOptions.CurrentValue.Login,
                    Password = relayOptions.CurrentValue.Password,
                    AutomaticEmails = relayOptions.CurrentValue.AutomaticEmails,
                    SenderAddress = relayOptions.CurrentValue.SenderAddress
                }
            };
        }

        [HttpPost]
        public void UpdateServer(Server serverUpdate)
        {
            ServerOptions newSettings = serverOptions.CurrentValue;
            RelayOptions newRelaySettings = relayOptions.CurrentValue;

            newSettings.Port = serverUpdate.PortNumber;
            newSettings.HostName = serverUpdate.HostName;
            newSettings.AllowRemoteConnections = serverUpdate.AllowRemoteConnections;
            newSettings.NumberOfMessagesToKeep = serverUpdate.NumberOfMessagesToKeep;
            newSettings.NumberOfSessionsToKeep = serverUpdate.NumberOfSessionsToKeep;
            newSettings.ImapPort = serverUpdate.ImapPortNumber;

            newRelaySettings.SmtpServer = serverUpdate.RelayOptions.SmtpServer;
            newRelaySettings.SmtpPort = serverUpdate.RelayOptions.SmtpPort;
            newRelaySettings.TlsMode = Enum.Parse<SecureSocketOptions>(serverUpdate.RelayOptions.TlsMode);
            newRelaySettings.SenderAddress = serverUpdate.RelayOptions.SenderAddress;
            newRelaySettings.Login = serverUpdate.RelayOptions.Login;
            newRelaySettings.Password = serverUpdate.RelayOptions.Password;
            newRelaySettings.AutomaticEmails = serverUpdate.RelayOptions.AutomaticEmails;

            if (!serverUpdate.IsRunning && this.server.IsRunning)
            {
                this.server.Stop();
            }
            else if (serverUpdate.IsRunning && !this.server.IsRunning)
            {
                this.server.TryStart();
            }

            if (!serverUpdate.IsRunning && this.imapServer.IsRunning)
            {
                this.imapServer.Stop();
            }
            else if (serverUpdate.IsRunning && !this.imapServer.IsRunning)
            {
                this.imapServer.TryStart();
            }

            System.IO.File.WriteAllText(hostingEnvironmentHelper.GetSettingsFilePath(),
                JsonSerializer.Serialize(new { ServerOptions = newSettings, RelayOptions = newRelaySettings },
                    new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}