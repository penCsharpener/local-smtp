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

using HtmlAgilityPack;
using LocalSmtp.Server.Application.Extensions;
using LocalSmtp.Server.Application.Models;
using LocalSmtp.Server.Application.Repositories.Abstractions;
using LocalSmtp.Server.Application.Services.Abstractions;
using LocalSmtp.Server.Controllers.Attributes;
using LocalSmtp.Server.Extensions;
using LocalSmtp.Server.Infrastructure.Models;
using LocalSmtp.Shared.ApiModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using System.Linq.Dynamic.Core;
using MessageEntity = LocalSmtp.Server.Infrastructure.Models.Message;
using MessageModel = LocalSmtp.Shared.ApiModels.Message;

namespace LocalSmtp.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
[UseEtagFilter]
public class MessagesController : Controller
{
    public MessagesController(IMessagesRepository messagesRepository, ILocalSmtpServer server)
    {
        this.messagesRepository = messagesRepository;
        this.server = server;
    }

    private readonly IMessagesRepository messagesRepository;
    private readonly ILocalSmtpServer server;

    [HttpGet]
    public IEnumerable<MessageSummary> GetSummaries(string sortColumn = "receivedDate", bool sortIsDescending = true)
    {
        return messagesRepository.GetMessages(false).Include(m => m.Relays)
            .OrderBy(sortColumn + (sortIsDescending ? " DESC" : ""))
            .Select(m => m.ToMessageSummaryApiModel());
    }

    private MessageEntity? GetDbMessage(Guid id)
    {
        return messagesRepository.GetMessages(false).SingleOrDefault(m => m.Id == id) ??
               throw new FileNotFoundException($"Message with id {id} was not found.");
    }

    [HttpGet("{id}")]
    public MessageModel? GetMessage(Guid id)
    {
        return GetExtendedMessage(id);
    }

    [HttpPost("{id}")]
    public Task MarkMessageRead(Guid id)
    {
        return messagesRepository.MarkMessageRead(id);
    }

    [HttpGet("{id}/download")]
    [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 31556926)]
    public FileStreamResult DownloadMessage(Guid id)
    {
        var result = GetDbMessage(id);
        return new FileStreamResult(new MemoryStream(result.Data), "message/rfc822") { FileDownloadName = $"{id}.eml" };
    }

    [HttpPost("{id}/relay")]
    public IActionResult RelayMessage(Guid id, [FromBody] MessageRelayOptions options)
    {
        var message = GetDbMessage(id);
        var relayResult = server.TryRelayMessage(message,
            options?.OverrideRecipientAddresses?.Length > 0
                ? options?.OverrideRecipientAddresses.Select(a => MailboxAddress.Parse(a)).ToArray()
                : null);

        if (relayResult.Exceptions.Any())
        {
            var relayErrorSummary = string.Join(". ", relayResult.Exceptions.Select(e => e.Key.Address + ": " + e.Value.Message));
            return Problem("Failed to relay to recipients: " + relayErrorSummary);
        }
        if (relayResult.WasRelayed)
        {
            foreach (var relay in relayResult.RelayRecipients)
            {
                message.AddRelay(new MessageRelay { SendDate = relay.RelayDate, To = relay.Email });
            }
            messagesRepository.DbContext.SaveChanges();
        }
        return Ok();
    }

    [HttpGet("{id}/part/{partid}/content")]
    [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 31556926)]
    public FileStreamResult GetPartContent(Guid id, string partid)
    {
        return GetExtendedMessage(id).GetPartContent(partid);
    }

    [HttpGet("{id}/part/{partid}/source")]
    [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 31556926)]
    public string GetPartSource(Guid id, string partid)
    {
        return GetExtendedMessage(id).GetPartContentAsText(partid);
    }

    [HttpGet("{id}/part/{partid}/raw")]
    [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 31556926)]
    public string GetPartSourceRaw(Guid id, string partid)
    {
        return GetExtendedMessage(id).GetPartSource(partid);
    }

    [HttpGet("{id}/raw")]
    [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 31556926)]
    public string GetMessageSourceRaw(Guid id)
    {
        var message = GetExtendedMessage(id);
        return System.Text.Encoding.UTF8.GetString(message.Data);
    }

    [HttpGet("{id}/source")]
    [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 31556926)]
    public string GetMessageSource(Guid id)
    {
        var message = GetExtendedMessage(id);
        return message.MimeMessage.ToString();
    }

    [HttpGet("{id}/html")]
    [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 31556926)]
    public string GetMessageHtml(Guid id)
    {
        var message = GetExtendedMessage(id);

        var html = message.MimeMessage?.HtmlBody;

        if (html == null)
        {
            html = "<pre>" + HtmlDocument.HtmlEncode(message.MimeMessage?.TextBody ?? "") + "</pre>";
        }


        HtmlDocument doc = new();
        doc.LoadHtml(html);


        var imageElements = doc.DocumentNode.SelectNodes("//img[starts-with(@src, 'cid:')]");

        if (imageElements != null)
        {
            foreach (var imageElement in imageElements)
            {
                var cid = imageElement.Attributes["src"].Value.Replace("cid:", "", StringComparison.OrdinalIgnoreCase);

                var part = message.Parts.Flatten(p => p.ChildParts).SingleOrDefault(p => p.ContentId == cid);

                imageElement.Attributes["src"].Value = $"api/Messages/{id}/part/{part?.Id ?? "notfound"}/content";
            }
        }

        return doc.DocumentNode.OuterHtml;
    }

    [HttpDelete("{id}")]
    public async Task Delete(Guid id)
    {
        await messagesRepository.DeleteMessage(id);
    }

    [HttpDelete("*")]
    public async Task DeleteAll()
    {
        await messagesRepository.DeleteAllMessages();
    }

    private ExtendedMessage? GetExtendedMessage(Guid id)
    {
        return GetDbMessage(id).ToApiModel();
    }
}