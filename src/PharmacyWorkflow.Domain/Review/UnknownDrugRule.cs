namespace PharmacyWorkflow.Domain.Review;

/// <summary>약품 정보에 없는 약품을 알려, 다른 규칙이 적용되지 않았음을 표시합니다.</summary>
public sealed class UnknownDrugRule : IReviewRule
{
    private readonly IDrugCatalog _catalog;

    public UnknownDrugRule(IDrugCatalog catalog) => _catalog = catalog;

    public string Code => "UNKNOWN_DRUG";

    public IEnumerable<ReviewFinding> Evaluate(Prescription prescription)
    {
        foreach (var item in prescription.Items)
        {
            if (_catalog.FindByName(item.DrugName) is null)
            {
                yield return new ReviewFinding(
                    Code,
                    ReviewSeverity.Caution,
                    $"약품 '{item.DrugName}'은(는) 약품 정보에 없어 다른 검수 규칙을 적용할 수 없습니다.",
                    new[] { item.DrugName });
            }
        }
    }
}
