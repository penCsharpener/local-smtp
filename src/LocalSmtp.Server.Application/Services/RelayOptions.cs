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


using MailKit.Security;

namespace LocalSmtp.Server.Application.Services;

public class RelayOptions
{
    private int smtpPort = 25;
    public bool IsEnabled => !string.IsNullOrWhiteSpace(SmtpServer);

    public string? SmtpServer { get; set; } = default!;

    public int SmtpPort
    {
        get => smtpPort;
        set
        {

            GuardAgainstOutOfRange(value, nameof(SmtpPort), 1, 65535);
            smtpPort = value;
        }
    }

    public SecureSocketOptions TlsMode { get; set; } = SecureSocketOptions.Auto;

    public string[]? AutomaticEmails { get; set; } = default!;

    public string? SenderAddress { get; set; } = default!;

    public string? Login { get; set; } = default!;

    public string? Password { get; set; } = default!;

    //[Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string AutomaticEmailsString
    {
        get => string.Join(",", AutomaticEmails ?? Array.Empty<string>());
        set => AutomaticEmails = value.Split(',');
    }

    //source: decompiled from https://github.com/ardalis/GuardClauses/blob/main/src/GuardClauses/GuardAgainstOutOfRangeExtensions.cs
    private int GuardAgainstOutOfRange(int input, string parameterName, int rangeFrom, int rangeTo, string? message = null)
    {
        if (rangeFrom.CompareTo(rangeTo) > 0)
        {
            throw new ArgumentException(message ?? $"{nameof(rangeFrom)} should be less or equal than {nameof(rangeTo)}");
        }

        if (input.CompareTo(rangeFrom) < 0 || input.CompareTo(rangeTo) > 0)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentOutOfRangeException(parameterName, $"Input {parameterName} was out of range");
            }
            throw new ArgumentOutOfRangeException(message, (Exception?)null);
        }

        return input;
    }
}