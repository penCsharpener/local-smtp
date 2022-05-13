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
using LocalSmtp.Server.Application.Hubs;
using LocalSmtp.Server.Application.Services.Abstractions;
using LocalSmtp.Server.Infrastructure.Data;
using LocalSmtp.Server.Infrastructure.Models;
using MailKit.Net.Smtp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Rnwood.SmtpServer;
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;

namespace LocalSmtp.Server.Application.Services;

public class LocalSmtpServer : ILocalSmtpServer
{
    private readonly ILogger _logger;

    public LocalSmtpServer(IServiceScopeFactory serviceScopeFactory, IOptionsMonitor<ServerOptions> serverOptions,
        IOptionsMonitor<RelayOptions> relayOptions, NotificationsHub notificationsHub, Func<RelayOptions, SmtpClient> relaySmtpClientFactory,
        ITaskQueue taskQueue, ILogger<LocalSmtpServer> logger)
    {
        _notificationsHub = notificationsHub;
        _serverOptions = serverOptions;
        _relayOptions = relayOptions;
        _serviceScopeFactory = serviceScopeFactory;
        _relaySmtpClientFactory = relaySmtpClientFactory;
        _taskQueue = taskQueue;
        _logger = logger;

        DoCleanup();

        IDisposable eventHandler = null;
        var obs = Observable.FromEvent<ServerOptions>(e => eventHandler = serverOptions.OnChange(e), e => eventHandler.Dispose());
        obs.Throttle(TimeSpan.FromMilliseconds(100)).Subscribe(OnServerOptionsChanged);

        taskQueue.Start();
    }

    private void OnServerOptionsChanged(ServerOptions arg1)
    {
        if (_smtpServer?.IsRunning == true)
        {
            _logger.LogInformation("ServerOptions changed. Restarting server...");
            Stop();
            TryStart();
        }
        else
        {
            _logger.LogInformation("ServerOptions changed.");
        }
    }

    private void CreateSmtpServer()
    {
        var cert = GetTlsCertificate();

        var serverOptionsValue = _serverOptions.CurrentValue;
        _smtpServer = new DefaultServer(serverOptionsValue.AllowRemoteConnections, serverOptionsValue.HostName, serverOptionsValue.Port,
            serverOptionsValue.TlsMode == TlsMode.ImplicitTls ? cert : null,
            serverOptionsValue.TlsMode == TlsMode.StartTls ? cert : null
        );
        _smtpServer.MessageReceivedEventHandler += OnMessageReceived;
        _smtpServer.SessionCompletedEventHandler += OnSessionCompleted;
        _smtpServer.SessionStartedHandler += OnSessionStarted;
        _smtpServer.AuthenticationCredentialsValidationRequiredEventHandler += OnAuthenticationCredentialsValidationRequired;
        _smtpServer.IsRunningChanged += (_, __) =>
        {
            if (_smtpServer.IsRunning)
                return;
            _logger.LogInformation("SMTP server stopped.");
            _notificationsHub.OnServerChanged().Wait();
        };
    }

    public void Stop()
    {
        _logger.LogInformation("SMTP server stopping...");
        _smtpServer.Stop(true);
    }

    private X509Certificate2 GetTlsCertificate()
    {
        X509Certificate2 cert = null;

        _logger.LogInformation("TLS mode: {TLSMode}", _serverOptions.CurrentValue.TlsMode);

        if (_serverOptions.CurrentValue.TlsMode != TlsMode.None)
        {
            if (!string.IsNullOrEmpty(_serverOptions.CurrentValue.TlsCertificate))
            {
                if (string.IsNullOrEmpty(_serverOptions.CurrentValue.TlsCertificatePrivateKey))
                {
                    cert = CertificateHelper.LoadCertificate(_serverOptions.CurrentValue.TlsCertificate);
                }
                else
                {
                    cert = CertificateHelper.LoadCertificateWithKey(_serverOptions.CurrentValue.TlsCertificate,
                        _serverOptions.CurrentValue.TlsCertificatePrivateKey);
                }

                _logger.LogInformation("Using provided certificate with Subject {SubjectName}, expiry {ExpiryDate}", cert.SubjectName.Name,
                    cert.GetExpirationDateString());
            }
            else
            {
                var pfxPath = Path.GetFullPath("selfsigned-certificate.pfx");
                var cerPath = Path.GetFullPath("selfsigned-certificate.cer");

                if (File.Exists(pfxPath))
                {
                    cert = new X509Certificate2(File.ReadAllBytes(pfxPath), "",
                        X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

                    if (cert.Subject != $"CN={_serverOptions.CurrentValue.HostName}" ||
                        DateTime.Parse(cert.GetExpirationDateString()) < DateTime.Now.AddDays(30))
                    {
                        cert = null;
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Using existing self-signed certificate with subject name {Hostname} and expiry date {ExpirationDate}",
                            _serverOptions.CurrentValue.HostName,
                            cert.GetExpirationDateString());
                    }
                }

                if (cert == null)
                {
                    cert = SSCertGenerator.CreateSelfSignedCertificate(_serverOptions.CurrentValue.HostName);
                    File.WriteAllBytes(pfxPath, cert.Export(X509ContentType.Pkcs12));
                    File.WriteAllBytes(cerPath, cert.Export(X509ContentType.Cert));
                    _logger.LogInformation("Generated new self-signed certificate with subject name '{Hostname} and expiry date {ExpirationDate}",
                        _serverOptions.CurrentValue.HostName,
                        cert.GetExpirationDateString());
                }


                _logger.LogInformation(
                    "Ensure that the hostname you enter into clients and '{Hostname}' from ServerOptions:HostName configuration match exactly and trust the issuer certificate at {cerPath} in your client/OS to avoid certificate validation errors.",
                    _serverOptions.CurrentValue.HostName, cerPath);
            }
        }

        return cert;
    }

    private void DoCleanup()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<AppDbContext>();

        foreach (var unfinishedSession in dbContext.Sessions.Where(s => !s.EndDate.HasValue).ToArray())
        {
            unfinishedSession.EndDate = DateTime.Now;
        }

        dbContext.SaveChanges();

        TrimMessages(dbContext);
        dbContext.SaveChanges();

        TrimSessions(dbContext);
        dbContext.SaveChanges();

        _notificationsHub.OnMessagesChanged().Wait();
        _notificationsHub.OnSessionsChanged().Wait();
    }

    private Task OnAuthenticationCredentialsValidationRequired(object sender, AuthenticationCredentialsValidationEventArgs e)
    {
        e.AuthenticationResult = AuthenticationResult.Success;
        return Task.CompletedTask;
    }


    private readonly IOptionsMonitor<ServerOptions> _serverOptions;
    private readonly IOptionsMonitor<RelayOptions> _relayOptions;
    private readonly IDictionary<ISession, Guid> activeSessionsToDbId = new Dictionary<ISession, Guid>();

    private static async Task UpdateDbSession(ISession session, Session dbSession)
    {
        dbSession.StartDate = session.StartDate;
        dbSession.EndDate = session.EndDate;
        dbSession.ClientAddress = session.ClientAddress.ToString();
        dbSession.ClientName = session.ClientName;
        dbSession.NumberOfMessages = (await session.GetMessages()).Count;
        dbSession.Log = (await session.GetLog()).ReadToEnd();
        dbSession.SessionErrorType = (InternalSessionErrorType)session.SessionErrorType;
        dbSession.SessionError = session.SessionError?.ToString();
    }

    private async Task OnSessionStarted(object sender, SessionEventArgs e)
    {
        _logger.LogInformation("Session started. Client address {clientAddress}.", e.Session.ClientAddress);
        await _taskQueue.QueueTask(() =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<AppDbContext>();

            var dbSession = new Session();
            UpdateDbSession(e.Session, dbSession).Wait();
            dbContext.Sessions.Add(dbSession);
            dbContext.SaveChanges();

            activeSessionsToDbId[e.Session] = dbSession.Id;
        }, false).ConfigureAwait(false);
    }

    private async Task OnSessionCompleted(object sender, SessionEventArgs e)
    {
        var messageCount = (await e.Session.GetMessages()).Count;
        _logger.LogInformation("Session completed. Client address {clientAddress}. Number of messages {messageCount}.", e.Session.ClientAddress,
            messageCount);

        await _taskQueue.QueueTask(() =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<AppDbContext>();

            var dbSession = dbContext.Sessions.Find(activeSessionsToDbId[e.Session]);
            UpdateDbSession(e.Session, dbSession).Wait();
            dbContext.SaveChanges();

            TrimSessions(dbContext);
            dbContext.SaveChanges();

            activeSessionsToDbId.Remove(e.Session);

            _notificationsHub.OnSessionsChanged().Wait();
        }, false).ConfigureAwait(false);
    }


    public Task DeleteSession(Guid id)
    {
        return _taskQueue.QueueTask(() =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<AppDbContext>();
            var session = dbContext.Sessions.SingleOrDefault(s => s.Id == id);
            if (session != null)
            {
                dbContext.Sessions.Remove(session);
                dbContext.SaveChanges();
                _notificationsHub.OnSessionsChanged().Wait();
            }
        }, true);
    }

    public Task DeleteAllSessions()
    {
        return _taskQueue.QueueTask(() =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<AppDbContext>();
            dbContext.Sessions.RemoveRange(dbContext.Sessions.Where(s => s.EndDate.HasValue));
            dbContext.SaveChanges();
            _notificationsHub.OnSessionsChanged().Wait();
        }, true);
    }

    private async Task OnMessageReceived(object sender, MessageEventArgs e)
    {
        var message = new MessageConverter().ConvertAsync(e.Message).Result;
        _logger.LogInformation("Message received. Client address {clientAddress}, From {messageFrom}, To {messageTo}, SecureConnection: {secure}.",
            e.Message.Session.ClientAddress, e.Message.From, message.To, e.Message.SecureConnection);
        message.IsUnread = true;

        await _taskQueue.QueueTask(() =>
        {
            _logger.LogInformation("Processing received message");
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<AppDbContext>();

            var relayResult = TryRelayMessage(message, null);
            message.RelayError = string.Join("\n", relayResult.Exceptions.Select(e => e.Key + ": " + e.Value.Message));

            var imapState = dbContext.ImapState.Single();
            imapState.LastUid = Math.Max(0, imapState.LastUid + 1);
            message.ImapUid = imapState.LastUid;
            message.Session = dbContext.Sessions.Find(activeSessionsToDbId[e.Message.Session]);
            if (relayResult.WasRelayed)
            {
                foreach (var relay in relayResult.RelayRecipients)
                {
                    message.AddRelay(new MessageRelay { SendDate = DateTime.UtcNow, To = relay.Email });
                }
            }

            dbContext.Messages.Add(message);

            dbContext.SaveChanges();

            TrimMessages(dbContext);
            dbContext.SaveChanges();
            _notificationsHub.OnMessagesChanged().Wait();
            _logger.LogInformation("Processing received message DONE");
        }, false).ConfigureAwait(false);
    }

    public RelayResult TryRelayMessage(Message message, MailboxAddress[] overrideRecipients)
    {
        var result = new RelayResult(message);

        if (!_relayOptions.CurrentValue.IsEnabled)
        {
            return result;
        }

        MailboxAddress[] recipients;

        if (overrideRecipients == null)
        {
            recipients = message.To
                .Split(",")
                .Select(r => MailboxAddress.Parse(r))
                .Where(r => _relayOptions.CurrentValue.AutomaticEmails.Contains(r.Address, StringComparer.OrdinalIgnoreCase))
                .ToArray();
        }
        else
        {
            recipients = overrideRecipients;
        }

        foreach (var recipient in recipients)
        {
            try
            {
                _logger.LogInformation("Relaying message to {recipient}", recipient);

                using var relaySmtpClient = _relaySmtpClientFactory(_relayOptions.CurrentValue);
                var apiMsg = message.ToApiModel();
                var newEmail = apiMsg.MimeMessage;
                var sender = MailboxAddress.Parse(
                    !string.IsNullOrEmpty(_relayOptions.CurrentValue.SenderAddress)
                        ? _relayOptions.CurrentValue.SenderAddress
                        : apiMsg.From);
                relaySmtpClient.Send(newEmail, sender, new[] { recipient });
                result.RelayRecipients.Add(new RelayRecipientResult() { Email = recipient.Address, RelayDate = DateTime.UtcNow });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Can not relay message to {recipient}: {errorMessage}", recipient, e.ToString());
                result.Exceptions[recipient] = e;
            }
        }

        return result;
    }

    private void TrimMessages(AppDbContext dbContext)
    {
        dbContext.Messages.RemoveRange(dbContext.Messages.OrderByDescending(m => m.ReceivedDate)
            .Skip(_serverOptions.CurrentValue.NumberOfMessagesToKeep));
    }

    private void TrimSessions(AppDbContext dbContext)
    {
        dbContext.Sessions.RemoveRange(dbContext.Sessions.Where(s => s.EndDate.HasValue).OrderByDescending(m => m.EndDate)
            .Skip(_serverOptions.CurrentValue.NumberOfSessionsToKeep));
    }


    private readonly ITaskQueue _taskQueue;
    private DefaultServer _smtpServer;
    private readonly Func<RelayOptions, SmtpClient> _relaySmtpClientFactory;
    private readonly NotificationsHub _notificationsHub;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public Exception Exception { get; private set; }

    public bool IsRunning
    {
        get { return _smtpServer.IsRunning; }
    }

    public int PortNumber
    {
        get { return _smtpServer.PortNumber; }
    }

    public void TryStart()
    {
        try
        {
            Exception = null;

            CreateSmtpServer();
            _smtpServer.Start();

            _logger.LogInformation("SMTP Server is listening on port {smtpPortNumber}.",
                _smtpServer.PortNumber);
            _logger.LogInformation("Keeping last {messagesToKeep} messages and {sessionsToKeep} sessions.",
                _serverOptions.CurrentValue.NumberOfMessagesToKeep, _serverOptions.CurrentValue.NumberOfSessionsToKeep);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "The SMTP server failed to start: {failureReason}", e.ToString());
            Exception = e;
        }
        finally
        {
            _notificationsHub.OnServerChanged().Wait();
        }
    }
}