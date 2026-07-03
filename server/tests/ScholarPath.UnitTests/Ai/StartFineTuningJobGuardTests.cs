using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Options;
using NSubstitute;
using ScholarPath.Application.Ai.Commands.FineTuning;
using ScholarPath.Application.Ai.Common;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using Xunit;

namespace ScholarPath.UnitTests.Ai;

/// <summary>
/// DES-04 — the admin fine-tuning pipeline is dormant by default. Starting a job
/// with <c>Ai:FineTuningEnabled=false</c> must be refused before any Azure call,
/// so a disabled platform never spends on (or polls) a fine-tuning run.
/// </summary>
public sealed class StartFineTuningJobGuardTests
{
    [Fact]
    public async Task Start_is_refused_when_fine_tuning_is_disabled()
    {
        var db = Substitute.For<IApplicationDbContext>();
        var fineTuning = Substitute.For<IAzureFineTuningService>();
        var mediator = Substitute.For<IMediator>();
        var opts = Options.Create(new AiCostOptionsSnapshot { FineTuningEnabled = false });

        var sut = new StartFineTuningJobCommandHandler(db, fineTuning, mediator, opts);

        var act = () => sut.Handle(new StartFineTuningJobCommand(), default);

        (await act.Should().ThrowAsync<ConflictException>())
            .Which.Message.Should().Contain("disabled");

        // The guard runs first — nothing was uploaded to Azure and no dataset was
        // exported.
        await fineTuning.DidNotReceive()
            .UploadTrainingFileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await mediator.DidNotReceive().Send(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
