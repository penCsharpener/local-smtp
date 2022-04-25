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
using Microsoft.AspNetCore.Mvc;
using MimeKit;

namespace LocalSmtp.Server.Extensions;

public static class MessageModelExtensions
{
    public static FileStreamResult GetPartContent(this ExtendedMessage? result, string cid)
    {
        var contentEntity = GetPart(result, cid);

        if (contentEntity is MimePart mimePart)
        {
            return new FileStreamResult(mimePart.Content.Open(), contentEntity.ContentType?.MimeType ?? "application/text")
            {
                FileDownloadName = mimePart.FileName
            };
        }
        else
        {
            MemoryStream? outputStream = new();
            contentEntity.WriteTo(outputStream, true);
            outputStream.Seek(0, SeekOrigin.Begin);

            return new FileStreamResult(outputStream, contentEntity.ContentType?.MimeType ?? "application/text");
        }
    }

    public static string GetPartContentAsText(this ExtendedMessage? result, string id)
    {
        var contentEntity = GetPart(result, id);

        if (contentEntity is MimePart part)
        {
            using StreamReader? reader = new(part.Content.Open());
            return reader.ReadToEnd();
        }

        return contentEntity.ToString();

    }

    public static string GetPartSource(this ExtendedMessage? message, string id)
    {
        var contentEntity = GetPart(message, id);
        return contentEntity.ToString();
    }

    private static MimeEntity GetPart(ExtendedMessage? message, string id)
    {
        if (message == null)
        {
            throw new FileNotFoundException($"Message not found");
        }

        var part = message.ExtendedParts.Flatten(p => p.ExtendedChildParts).SingleOrDefault(p => p.Id == id);

        if (part == null)
        {
            throw new FileNotFoundException($"Part with id '{id}' in message {message.Id} is not found");
        }

        return part.MimeEntity;
    }
}
