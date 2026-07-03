using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Documents;

/// <summary>
/// FR-ONB-13 — the onboarding verification documents each role must upload before
/// its request reaches the admin approval queue. Shared by first-time onboarding
/// (SelectRole) and the Student→Consultant upgrade so both enforce the same bar.
/// </summary>
public static class OnboardingDocumentRequirements
{
    public static readonly IReadOnlyList<OnboardingDocumentType> ScholarshipProvider =
    [
        OnboardingDocumentType.ProviderLegalRegistration,
        OnboardingDocumentType.ProviderAuthorizedRepresentativeId,
    ];

    public static readonly IReadOnlyList<OnboardingDocumentType> Consultant =
    [
        OnboardingDocumentType.ConsultantIdentityProof,
        OnboardingDocumentType.ConsultantDegreeCertificate,
        OnboardingDocumentType.ConsultantCvResume,
    ];

    public static IReadOnlyList<OnboardingDocumentType> RequiredFor(string role) => role switch
    {
        "ScholarshipProvider" => ScholarshipProvider,
        "Consultant" => Consultant,
        _ => [],
    };

    /// <summary>The required types that are NOT present in the uploaded set.</summary>
    public static IReadOnlyList<OnboardingDocumentType> MissingTypes(
        string role, IEnumerable<OnboardingDocumentType> uploadedTypes)
    {
        var have = uploadedTypes.ToHashSet();
        return RequiredFor(role).Where(r => !have.Contains(r)).ToList();
    }
}
