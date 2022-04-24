// source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/Server

using MailKit.Security;

namespace LocalSmtp.Server.Application.Services;

public class RelayOptions
{
    private int smtpPort = 25;
    public bool IsEnabled => SmtpServer != string.Empty;

    public string SmtpServer { get; set; } = string.Empty;

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

    public string[] AutomaticEmails { get; set; } = Array.Empty<string>();

    public string SenderAddress { get; set; } = "";

    public string Login { get; set; } = "";

    public string Password { get; set; } = "";

    //[Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string AutomaticEmailsString
    {
        get => string.Join(",", AutomaticEmails);
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