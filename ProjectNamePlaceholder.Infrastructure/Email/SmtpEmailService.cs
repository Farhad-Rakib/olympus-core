using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Application.Common.Interfaces.Services;

namespace ProjectNamePlaceholder.Infrastructure.Email;

public sealed class SmtpEmailService : IEmailService
{
    private readonly SmtpOptions _smtpOptions;

    public SmtpEmailService(IOptions<SmtpOptions> smtpOptions)
    {
        _smtpOptions = smtpOptions.Value;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_smtpOptions.FromAddress, _smtpOptions.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(new MailAddress(to));

        using var client = new SmtpClient(_smtpOptions.Host, _smtpOptions.Port)
        {
            EnableSsl = _smtpOptions.UseSsl,
            Credentials = new NetworkCredential(_smtpOptions.Username, _smtpOptions.Password)
        };

        using var registration = cancellationToken.Register(client.SendAsyncCancel);
        await client.SendMailAsync(message, cancellationToken);
    }
}
