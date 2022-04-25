/*
Copyright(c) 2009 - 2018, smtp4dev project contributors All rights reserved.
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
    * Neither the name of smtp4dev nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS;
OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/ApiModels
*/

using LocalSmtp.Server.Application.Models;
using LocalSmtp.Shared.ApiModels;
using MimeKit;
using HeaderModel = LocalSmtp.Shared.ApiModels.Header;
using MessageEntity = LocalSmtp.Server.Infrastructure.Models.Message;
using SessionEntity = LocalSmtp.Server.Infrastructure.Models.Session;

namespace LocalSmtp.Server.Application.Extensions;

public static class MappingExtensions
{
    public static ExtendedMessage? ToApiModel(this MessageEntity? entity)
    {
        if (entity is null)
        {
            return null;
        }

        ExtendedMessage? model = new()
        {
            Id = entity.Id,
            From = entity.From,
            To = entity.To,
            Cc = "",
            Bcc = "",
            ReceivedDate = entity.ReceivedDate,
            Subject = entity.Subject,
            SecureConnection = entity.SecureConnection,
            ExtendedParts = new List<ExtendedMessageEntitySummary>(1),
            RelayError = entity.RelayError,
            Data = entity.Data
        };

        if (entity.MimeParseError != null)
        {
            model.MimeParseError = entity.MimeParseError;
            model.Headers = new List<HeaderModel>(0);
        }
        else
        {
            using MemoryStream? stream = new(entity.Data);
            model.MimeMessage = MimeMessage.Load(stream);

            if (model.MimeMessage.From != null)
            {
                model.From = model.MimeMessage.From.ToString();
            }

            List<string>? recipients = new(entity.To.Split(",")
                .Select(r => r.Trim())
                .Where(r => !string.IsNullOrEmpty(r)));

            if (model.MimeMessage.To != null)
            {
                model.To = string.Join(", ", model.MimeMessage.To.Select(t => t.ToString().DecodeIdnMapping()));

                foreach (var internetAddress in model.MimeMessage.To.Where(t => t is MailboxAddress))
                {
                    var to = (MailboxAddress)internetAddress;
                    recipients.Remove(to.Address);
                }
            }

            if (model.MimeMessage.Cc != null)
            {
                model.Cc = string.Join(", ", model.MimeMessage.Cc.Select(t => t.ToString().DecodeIdnMapping()));

                foreach (var internetAddress in model.MimeMessage.Cc.Where(t => t is MailboxAddress))
                {
                    var cc = (MailboxAddress)internetAddress;
                    recipients.Remove(cc.Address);
                }
            }

            model.Bcc = string.Join(", ", recipients);

            model.Headers = model.MimeMessage.Headers.Select(h => new HeaderModel { Name = h.Field, Value = h.Value.DecodeIdnMapping() }).ToList();
            model.ExtendedParts.Add(HandleMimeEntity(model.MimeMessage.Body, model));
            model.Parts = model.ExtendedParts.Cast<MessageEntitySummary>().ToList();
        }

        return model;
    }

    public static MessageSummary ToMessageSummaryApiModel(this MessageEntity entity)
    {
        return new()
        {
            Id = entity.Id,
            From = entity.From,
            To = entity.To,
            ReceivedDate = entity.ReceivedDate,
            Subject = entity.Subject,
            AttachmentCount = entity.AttachmentCount,
            IsUnread = entity.IsUnread,
            IsRelayed = entity.Relays.Count > 0
        };
    }

    public static SessionSummary ToSummaryApiModel(this SessionEntity entity)
    {
        return new()
        {
            ClientAddress = entity.ClientAddress,
            ClientName = entity.ClientName,
            NumberOfMessages = entity.NumberOfMessages,
            Id = entity.Id,
            EndDate = entity.EndDate,
            StartDate = entity.StartDate,
            TerminatedWithError = entity.SessionError != null,
            Size = entity.Log?.Length ?? 0
        };
    }

    public static Session ToApiModel(this SessionEntity entity)
    {
        return new()
        {
            Id = entity.Id,
            Error = entity.SessionError,
            ErrorType = entity.SessionErrorType?.ToString()
        };
    }

    private static ExtendedMessageEntitySummary HandleMimeEntity(MimeEntity entity, ExtendedMessage model)
    {
        var index = 0;

        return MimeEntityVisitor.VisitWithResults<ExtendedMessageEntitySummary>(entity, (e, p) =>
        {
            var fileName = (!string.IsNullOrEmpty(e.ContentDisposition?.FileName) ? e.ContentDisposition?.FileName : e.ContentType?.Name).DecodeIdnMapping();

            ExtendedMessageEntitySummary? result = new()
            {
                MessageId = model.Id,
                Id = index.ToString(),
                ContentId = e.ContentId,
                Name = (fileName ?? e.ContentId ?? index.ToString()) + " - " + e.ContentType.MimeType,
                Headers = e.Headers.Select(h => new HeaderModel { Name = h.Field, Value = h.Value.DecodeIdnMapping() }).ToList(),
                ExtendedChildParts = new List<ExtendedMessageEntitySummary>(),
                Attachments = new List<AttachmentSummary>(),
                Warnings = new List<MessageWarning>(),
                Size = e.ToString().Length,
                IsAttachment = (e.ContentDisposition?.Disposition != "inline" && !string.IsNullOrEmpty(fileName)) || e.ContentDisposition?.Disposition == "attachment",
                MimeEntity = e
            };

            if (p != null)
            {
                p.ExtendedChildParts.Add(result);

                if (result.IsAttachment)
                {
                    if (e.ContentDisposition?.Disposition != "attachment")
                    {
                        result.Warnings.Add(new MessageWarning { Details = $"Attachment '{fileName}' should have \"Content-Disposition: attachment\" header." });
                    }

                    if (string.IsNullOrEmpty(fileName))
                    {
                        result.Warnings.Add(new MessageWarning { Details = $"Attachment with content ID '{e.ContentId}' should have filename specified in either 'Content-Type' or 'Content-Disposition' header." });
                    }

                    p.Attachments.Add(new AttachmentSummary()
                    {
                        Id = result.Id,
                        ContentId = result.ContentId,
                        FileName = fileName,
                        Url = $"api/messages/{model.Id}/part/{result.Id}/content"
                    });
                }
            }

            index++;

            result.ChildParts = result.ExtendedChildParts.Cast<MessageEntitySummary>().ToList();

            return result;
        });

    }
}
