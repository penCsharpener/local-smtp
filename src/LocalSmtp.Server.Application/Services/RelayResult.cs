﻿/*
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


using LocalSmtp.Server.Infrastructure.Models;
using MimeKit;

namespace LocalSmtp.Server.Application.Services;

public class RelayResult
{
    public RelayResult(Message message)
    {
        Message = message;
    }

    public Dictionary<MailboxAddress, Exception> Exceptions { get; set; } = new Dictionary<MailboxAddress, Exception>();
    public List<RelayRecipientResult> RelayRecipients { get; set; } = new List<RelayRecipientResult>();
    public bool WasRelayed => RelayRecipients.Count > 0;
    public Message Message { get; set; }
}