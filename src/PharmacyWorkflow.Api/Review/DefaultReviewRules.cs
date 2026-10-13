using PharmacyWorkflow.Domain.Review;

namespace PharmacyWorkflow.Api.Review;

/// <summary>앱과 테스트가 함께 쓰는 기본 규칙 구성. 규칙을 추가할 때는 여기에 한 줄만 더하면 됩니다.</summary>
public static class DefaultReviewRules
{
    public static IReadOnlyList<IReviewRule> Create(IDrugCatalog catalog) => new IReviewRule[]
    {
        new UnknownDrugRule(catalog),
        new MaxDailyDoseRule(catalog),
        new DuplicateIngredientRule(catalog),
        new DrugInteractionRule(catalog),
    };
}
