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
using ServerApiModel = LocalSmtp.Shared.ApiModels.Server;

namespace LocalSmtp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServerController : Controller
    {

        private readonly ILocalSmtpServer _server;
        private readonly ImapServer _imapServer;
        private readonly IOptionsMonitor<ServerOptions> _serverOptions;
        private readonly IOptionsMonitor<RelayOptions> _relayOptions;
        private readonly IHostingEnvironmentHelper _hostingEnvironmentHelper;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

        public ServerController(ILocalSmtpServer server, ImapServer imapServer, IOptionsMonitor<ServerOptions> serverOptions,
            IOptionsMonitor<RelayOptions> relayOptions, IHostingEnvironmentHelper hostingEnvironmentHelper)
        {
            _server = server;
            _imapServer = imapServer;
            _serverOptions = serverOptions;
            _relayOptions = relayOptions;
            _hostingEnvironmentHelper = hostingEnvironmentHelper;
        }

        [HttpGet]
        public ServerApiModel GetServer()
        {
            return new ServerApiModel()
            {
                IsRunning = _server.IsRunning,
                PortNumber = _serverOptions.CurrentValue.Port,
                ImapPortNumber = _serverOptions.CurrentValue.ImapPort,
                HostName = _serverOptions.CurrentValue.HostName,
                AllowRemoteConnections = _serverOptions.CurrentValue.AllowRemoteConnections,
                NumberOfMessagesToKeep = _serverOptions.CurrentValue.NumberOfMessagesToKeep,
                NumberOfSessionsToKeep = _serverOptions.CurrentValue.NumberOfSessionsToKeep,
                Exception = _server.Exception?.Message,
                RelayOptions = new ServerRelayOptions
                {
                    SmtpServer = _relayOptions.CurrentValue.SmtpServer,
                    TlsMode = _relayOptions.CurrentValue.TlsMode.ToString(),
                    SmtpPort = _relayOptions.CurrentValue.SmtpPort,
                    Login = _relayOptions.CurrentValue.Login,
                    Password = _relayOptions.CurrentValue.Password,
                    AutomaticEmails = _relayOptions.CurrentValue.AutomaticEmails,
                    SenderAddress = _relayOptions.CurrentValue.SenderAddress
                }
            };
        }

        [HttpPost]
        public void UpdateServer(ServerApiModel serverUpdate)
        {
            var newSettings = _serverOptions.CurrentValue;
            var newRelaySettings = _relayOptions.CurrentValue;

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

            if (!serverUpdate.IsRunning && _server.IsRunning)
            {
                _server.Stop();
            }
            else if (serverUpdate.IsRunning && !_server.IsRunning)
            {
                _server.TryStart();
            }

            if (!serverUpdate.IsRunning && _imapServer.IsRunning)
            {
                _imapServer.Stop();
            }
            else if (serverUpdate.IsRunning && !_imapServer.IsRunning)
            {
                _imapServer.TryStart();
            }

            var json = JsonSerializer.Serialize(new { ServerOptions = newSettings, RelayOptions = newRelaySettings }, _jsonSerializerOptions);

            System.IO.File.WriteAllText(_hostingEnvironmentHelper.GetSettingsFilePath(), json);
        }
    }
}