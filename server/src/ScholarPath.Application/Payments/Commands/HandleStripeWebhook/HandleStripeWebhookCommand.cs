using MediatR;

namespace ScholarPath.Application.Payments.Commands.HandleStripeWebhook;

public sealed record HandleStripeWebhookCommand(
    string Payload,
    string? SignatureHeader
) : IRequest;
