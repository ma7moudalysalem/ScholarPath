using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Models;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Unit>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly AppSettings _appSettings;

    public ForgotPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IOptions<AppSettings> appSettings)
    {
        _userManager = userManager;
        _emailService = emailService;
        _appSettings = appSettings.Value;
    }

    public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
            return Unit.Value;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var resetLink = $"{_appSettings.ClientUrl}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email!)}";

        await _emailService.SendPasswordResetEmailAsync(user.Email!, resetLink, cancellationToken);

        return Unit.Value;
    }
}
