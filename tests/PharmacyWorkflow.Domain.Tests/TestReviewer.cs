using PharmacyWorkflow.Api.Review;
using PharmacyWorkflow.Domain.Review;

namespace PharmacyWorkflow.Domain.Tests;

/// <summary>테스트에서 앱과 같은 규칙 구성을 쉽게 만들기 위한 도우미.</summary>
internal static class TestReviewer
{
    public static PrescriptionReviewer Create() =>
        new(DefaultReviewRules.Create(new SampleDrugCatalog()));
}
