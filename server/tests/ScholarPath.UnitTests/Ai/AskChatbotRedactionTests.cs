using ScholarPath.Application.Ai.Commands.AskChatbot;

namespace ScholarPath.UnitTests.Ai;

public class AskChatbotRedactionTests
{
    [Theory]
    [InlineData("email me at mahmoud@example.com thanks", "[redacted-email]")]
    [InlineData("call +1 555 234 5678 please", "[redacted-phone]")]
    [InlineData("my card is 4242 4242 4242 4242", "[redacted-card]")]
    public void Redacts_common_PII_patterns(string input, string expectedToken)
    {
        var redacted = AskChatbotCommandHandler.RedactPii(input);
        redacted.Should().Contain(expectedToken);
    }

    [Fact]
    public void Leaves_safe_text_untouched()
    {
        const string msg = "tell me about fully-funded master's programs in Germany";
        AskChatbotCommandHandler.RedactPii(msg).Should().Be(msg);
    }

    [Fact]
    public void Handles_empty()
    {
        AskChatbotCommandHandler.RedactPii("").Should().BeEmpty();
    }
}

public class AskChatbotValidatorTests
{
    private readonly AskChatbotCommandValidator _v = new();

    [Fact]
    public void Empty_message_fails()
    {
        var r = _v.Validate(new AskChatbotCommand("", null));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Long_message_fails()
    {
        var big = new string('x', 2001);
        var r = _v.Validate(new AskChatbotCommand(big, null));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Normal_message_passes()
    {
        var r = _v.Validate(new AskChatbotCommand("how do I apply?", null));
        r.IsValid.Should().BeTrue();
    }
}
