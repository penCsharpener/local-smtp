// source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/Server

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
            using (StreamReader dataReader = new StreamReader(new MemoryStream(data)))
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
                    CancellationTokenSource cts = new CancellationTokenSource();
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

        Message result = new Message
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

        var parts = new Message(result).Parts;
        foreach (var part in parts)
        {
            result.AttachmentCount += CountAttachments(part);
        }

        return result;
    }

    private int CountAttachments(MessageEntitySummary part)
    {
        return part.Attachments.Count + part.ChildParts.Sum(CountAttachments);
    }
}