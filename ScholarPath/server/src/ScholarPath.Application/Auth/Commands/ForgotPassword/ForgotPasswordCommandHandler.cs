using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Unit>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public ForgotPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        // Always return success even if user not found (security: don't leak if email exists)
        if (user is null)
        {
            return Unit.Value;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var clientUrl = _configuration["App:ClientUrl"] ?? "http://localhost:3000";
        var resetLink = $"{clientUrl}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email!)}";

        await _emailService.SendPasswordResetEmailAsync(user.Email!, resetLink, cancellationToken);

        return Unit.Value;
    }
}
