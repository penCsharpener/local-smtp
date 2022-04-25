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
using LocalSmtp.Server.Infrastructure.Models;
using MimeKit;
using Rnwood.SmtpServer;

namespace LocalSmtp.Server.Application.Services;

public class MessageConverter
{
    public async Task<Message> ConvertAsync(IMessage message)
    {
        string subject = "";
        string mimeParseError = null;
        string toAddress = string.Join(", ", message.Recipients);

        byte[] data;
        using (Stream messageData = await message.GetData())
        {
            data = new byte[messageData.Length];
            await messageData.ReadAsync(data, 0, data.Length);

            bool foundHeaders = false;
            bool foundSeparator = false;
            using (StreamReader dataReader = new(new MemoryStream(data)))
            {
                while (!dataReader.EndOfStream)
                {
                    if (dataReader.ReadLine().Length != 0)
                    {
                        foundHeaders = true;
                    }
                    else
                    {
                        foundSeparator = true;
                        break;
                    }
                }
            }

            if (!foundHeaders || !foundSeparator)
            {
                mimeParseError = "Malformed MIME message. No headers found";
            }
            else
            {

                messageData.Seek(0, SeekOrigin.Begin);
                try
                {
                    CancellationTokenSource cts = new();
                    cts.CancelAfter(TimeSpan.FromSeconds(10));
                    MimeMessage mime = await MimeMessage.LoadAsync(messageData, true, cts.Token).ConfigureAwait(false);
                    subject = mime.Subject;


                }
                catch (OperationCanceledException e)
                {
                    mimeParseError = e.Message;
                }
                catch (FormatException e)
                {
                    mimeParseError = e.Message;
                }
            }
        }

        Message result = new()
        {
            Id = Guid.NewGuid(),
            From = message.From.DecodeIdnMapping(),
            To = toAddress.DecodeIdnMapping(),
            ReceivedDate = DateTime.Now,
            Subject = subject.DecodeIdnMapping(),
            Data = data,
            MimeParseError = mimeParseError,
            AttachmentCount = 0,
            SecureConnection = message.SecureConnection
        };

        List<Shared.ApiModels.MessageEntitySummary>? parts = result.ToApiModel().Parts;
        foreach (Shared.ApiModels.MessageEntitySummary? part in parts)
        {
            result.AttachmentCount += CountAttachments(part);
        }

        return result;
    }

    private int CountAttachments(Shared.ApiModels.MessageEntitySummary part)
    {
        return part.Attachments.Count + part.ChildParts.Sum(CountAttachments);
    }
}