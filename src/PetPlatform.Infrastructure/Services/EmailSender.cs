using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace PetPlatform.Infrastructure.Services;

public class EmailSender<TUser> : IEmailSender<TUser> where TUser : class
{
    private readonly ILogger<EmailSender<TUser>> _logger;

    public EmailSender(ILogger<EmailSender<TUser>> logger)
    {
        _logger = logger;
    }

    public Task SendConfirmationLinkAsync(TUser user, string email, string confirmationLink)
    {
        _logger.LogInformation("Confirmation link for {Email}: {Link}", email, confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(TUser user, string email, string resetCode)
    {
        _logger.LogInformation("Password reset code for {Email}: {Code}", email, resetCode);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(TUser user, string email, string resetLink)
    {
        _logger.LogInformation("Password reset link for {Email}: {Link}", email, resetLink);
        return Task.CompletedTask;
    }
}
