namespace ProjectNamePlaceholder.Application.Common.Interfaces.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
