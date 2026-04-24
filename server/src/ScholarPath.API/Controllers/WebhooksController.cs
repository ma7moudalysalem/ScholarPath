using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Payments.Commands.ProcessStripeWebhook;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.API.Controllers;

[ApiController]
[Route("api/webhooks/stripe")]
[AllowAnonymous]
public class WebhooksController(
    IMediator mediator,
    IStripeService stripeService,
    IOptions<StripeOptions> stripeOptions,
    ILogger<WebhooksController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> HandleStripeWebhook(CancellationToken ct)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(ct);
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(signature))
            return BadRequest();

        try
        {
            var parsed = stripeService.ParseWebhook(json, signature, stripeOptions.Value.WebhookSecret!);
            var command = new ProcessStripeWebhookCommand(parsed.EventId, parsed.EventType, parsed.DataJson);
            
            await mediator.Send(command, ct);
            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process Stripe webhook");
            return BadRequest();
        }
    }
}
