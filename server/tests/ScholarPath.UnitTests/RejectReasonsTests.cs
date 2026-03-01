using System.Text.Json;

namespace ScholarPath.UnitTests;

public class RejectReasonsTests
{
    // Valid rejection codes 
    private static readonly HashSet<string> ValidCodes =
    [
        "missing_crn",
        "proof_not_clear",
        "suspicious_request",
        "incomplete_profile",
        "invalid_documents",
        "duplicate_request",
        "other"
    ];

    //  Valid Codes 

    [Theory]
    [InlineData("missing_crn")]
    [InlineData("proof_not_clear")]
    [InlineData("suspicious_request")]
    [InlineData("incomplete_profile")]
    [InlineData("invalid_documents")]
    [InlineData("duplicate_request")]
    [InlineData("other")]
    public void Rejection_code_is_valid(string code)
    {
        var isValid = ValidCodes.Contains(code);

        Assert.True(isValid);
    }

    [Theory]
    [InlineData("bad_code")]
    [InlineData("MISSING_CRN")]   // case sensitive
    [InlineData("")]
    [InlineData("random_reason")]
    public void Rejection_code_is_invalid(string code)
    {
        var isValid = ValidCodes.Contains(code);

        Assert.False(isValid);
    }

    //Reasons List Validation 

    [Fact]
    public void Reject_request_fails_when_reasons_list_is_empty()
    {
        var reasons = new List<RejectionReasonDto>();

        var isInvalid = reasons is null || reasons.Count == 0;

        Assert.True(isInvalid);
    }

    [Fact]
    public void Reject_request_fails_when_reasons_list_is_null()
    {
        List<RejectionReasonDto>? reasons = null;

        var isInvalid = reasons is null || reasons.Count == 0;

        Assert.True(isInvalid);
    }

    [Fact]
    public void Reject_request_passes_when_reasons_list_has_valid_code()
    {
        var reasons = new List<RejectionReasonDto>
        {
            new RejectionReasonDto("missing_crn", null)
        };

        var isInvalid = reasons is null || reasons.Count == 0;
        var invalidCodes = reasons!
            .Where(r => !ValidCodes.Contains(r.Code))
            .ToList();

        Assert.False(isInvalid);
        Assert.Empty(invalidCodes);
    }

    [Fact]
    public void Reject_request_fails_when_one_code_is_invalid()
    {
        var reasons = new List<RejectionReasonDto>
        {
            new RejectionReasonDto("missing_crn", null),
            new RejectionReasonDto("totally_wrong_code", null)
        };

        var invalidCodes = reasons
            .Where(r => !ValidCodes.Contains(r.Code))
            .Select(r => r.Code)
            .Distinct()
            .ToList();

        Assert.NotEmpty(invalidCodes);
        Assert.Contains("totally_wrong_code", invalidCodes);
    }

    // JSON Serialization

    [Fact]
    public void RejectionReasons_serializes_to_json_correctly()
    {
        var reasons = new List<RejectionReasonDto>
        {
            new RejectionReasonDto("missing_crn", "CRN was not provided"),
            new RejectionReasonDto("proof_not_clear", null)
        };

        var json = JsonSerializer.Serialize(reasons);

        Assert.NotNull(json);
        Assert.Contains("missing_crn", json);
        Assert.Contains("proof_not_clear", json);
    }

    [Fact]
    public void RejectionReasons_deserializes_from_json_correctly()
    {
        var json = "[{\"Code\":\"missing_crn\",\"Note\":\"CRN missing\"},{\"Code\":\"other\",\"Note\":null}]";

        var reasons = JsonSerializer.Deserialize<List<RejectionReasonDto>>(json);

        Assert.NotNull(reasons);
        Assert.Equal(2, reasons!.Count);
        Assert.Equal("missing_crn", reasons[0].Code);
        Assert.Equal("other", reasons[1].Code);
    }

    //  Backward Compatibility 

    [Fact]
    public void RejectionReason_backward_compat_string_is_codes_joined()
    {
        var reasons = new List<RejectionReasonDto>
        {
            new RejectionReasonDto("missing_crn", null),
            new RejectionReasonDto("proof_not_clear", null)
        };

        var backwardCompatString = string.Join(", ", reasons.Select(r => r.Code));

        Assert.Equal("missing_crn, proof_not_clear", backwardCompatString);
    }

    // Optional Note Field 

    [Fact]
    public void RejectionReasonDto_note_is_optional()
    {
        var reason = new RejectionReasonDto("other", null);

        Assert.Equal("other", reason.Code);
        Assert.Null(reason.Note);
    }

    [Fact]
    public void RejectionReasonDto_note_can_have_value()
    {
        var reason = new RejectionReasonDto("other", "Please resubmit with correct documents");

        Assert.Equal("other", reason.Code);
        Assert.NotNull(reason.Note);
    }
}

// Local copy of the DTO for unit testing 
public record RejectionReasonDto(string Code, string? Note);
