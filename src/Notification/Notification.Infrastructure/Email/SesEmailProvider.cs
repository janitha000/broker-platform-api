using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Options;
using Notification.Application.Abstractions;

namespace Notification.Infrastructure.Email;

public sealed class SesEmailProvider : IEmailProvider
{
    private readonly EmailOptions _options;

    public SesEmailProvider(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task<EmailSendResult> Send(EmailMessage message, CancellationToken cancellationToken = default)
    {
        using var client = new AmazonSimpleEmailServiceClient(RegionEndpoint.GetBySystemName(_options.AwsRegion));
        try
        {
            var response = await client.SendEmailAsync(new SendEmailRequest
            {
                Source = _options.FromAddress,
                Destination = new Destination { ToAddresses = [message.To] },
                Message = new Message
                {
                    Subject = new Content(message.Subject),
                    Body = new Body { Text = new Content(message.Body) },
                },
            }, cancellationToken);

            return new EmailSendResult(true, response.MessageId, null, null);
        }
        catch (Exception ex)
        {
            return new EmailSendResult(false, null, ex.GetType().Name, ex.Message);
        }
    }
}