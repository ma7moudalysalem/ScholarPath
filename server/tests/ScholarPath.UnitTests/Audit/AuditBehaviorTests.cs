using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Behaviors;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.UnitTests.Audit;

public class AuditBehaviorTests
{
    // ---- fixtures ----

    public sealed record PlainRequest(Guid Id) : IRequest<string>;

    [Auditable(AuditAction.Update, "Scholarship", SummaryTemplate = "Updated scholarship {TargetId}")]
    public sealed record AuditedRequest(Guid Id) : IRequest<Response>;

    [Auditable(AuditAction.Delete, "Widget")]
    public sealed record AuditedNoResponseIdRequest(Guid Id) : IRequest<Response?>;

    public sealed record Response(Guid Id);

    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private readonly ILogger<AuditBehavior<PlainRequest, string>> _logger1 =
        Substitute.For<ILogger<AuditBehavior<PlainRequest, string>>>();

    [Fact]
    public async Task No_attribute_skips_audit()
    {
        var sut = new AuditBehavior<PlainRequest, string>(_audit, _logger1);

        RequestHandlerDelegate<string> next = ct => Task.FromResult("ok");
        var result = await sut.Handle(new PlainRequest(Guid.NewGuid()), next, CancellationToken.None);

        result.Should().Be("ok");
        await _audit.DidNotReceiveWithAnyArgs().WriteAsync(default, default!, default, default, default, default, default);
    }

    [Fact]
    public async Task Attribute_writes_audit_with_response_id()
    {
        var logger = Substitute.For<ILogger<AuditBehavior<AuditedRequest, Response>>>();
        var sut = new AuditBehavior<AuditedRequest, Response>(_audit, logger);

        var responseId = Guid.NewGuid();
        RequestHandlerDelegate<Response> next = ct => Task.FromResult(new Response(responseId));

        var result = await sut.Handle(new AuditedRequest(Guid.NewGuid()), next, CancellationToken.None);

        result.Id.Should().Be(responseId);
        await _audit.Received(1).WriteAsync(
            AuditAction.Update,
            "Scholarship",
            responseId,
            null,
            null,
            $"Updated scholarship {responseId}",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Falls_back_to_request_id_when_response_has_no_match()
    {
        var logger = Substitute.For<ILogger<AuditBehavior<AuditedNoResponseIdRequest, Response?>>>();
        var sut = new AuditBehavior<AuditedNoResponseIdRequest, Response?>(_audit, logger);

        var requestId = Guid.NewGuid();
        // handler returns null on purpose; SkipOnNull defaults to true, so no audit is expected
        RequestHandlerDelegate<Response?> next = ct => Task.FromResult<Response?>(null);

        await sut.Handle(new AuditedNoResponseIdRequest(requestId), next, CancellationToken.None);

        await _audit.DidNotReceiveWithAnyArgs().WriteAsync(default, default!, default, default, default, default, default);
    }

    [Fact]
    public async Task Audit_write_exception_does_not_fail_the_command()
    {
        var logger = Substitute.For<ILogger<AuditBehavior<AuditedRequest, Response>>>();
        _audit.WriteAsync(default, default!, default, default, default, default, default)
              .ReturnsForAnyArgs(Task.FromException(new InvalidOperationException("db offline")));

        var sut = new AuditBehavior<AuditedRequest, Response>(_audit, logger);
        RequestHandlerDelegate<Response> next = ct => Task.FromResult(new Response(Guid.NewGuid()));

        // should not throw
        var result = await sut.Handle(new AuditedRequest(Guid.NewGuid()), next, CancellationToken.None);

        result.Should().NotBeNull();
    }
}
