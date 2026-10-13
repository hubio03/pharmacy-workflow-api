namespace PharmacyWorkflow.Domain.Review;

/// <summary>
/// 1일 투여량 = 1회 투여 개수(DosePerTime) × 1일 횟수 × 1개당 함량(mg).
/// 약품별 최대 1일 투여량을 넘으면 중대 경고입니다.
/// </summary>
public sealed class MaxDailyDoseRule : IReviewRule
{
    private readonly IDrugCatalog _catalog;

    public MaxDailyDoseRule(IDrugCatalog catalog) => _catalog = catalog;

    public string Code => "MAX_DAILY_DOSE";

    public IEnumerable<ReviewFinding> Evaluate(Prescription prescription)
    {
        foreach (var item in prescription.Items)
        {
            var drug = _catalog.FindByName(item.DrugName);
            if (drug is null || drug.MaxDailyDoseMg is null)
                continue;

            var max = drug.MaxDailyDoseMg.Value;
            var daily = item.DosePerTime * item.TimesPerDay * drug.StrengthMg;

            if (daily > max)
            {
                yield return new ReviewFinding(
                    Code,
                    ReviewSeverity.Critical,
                    $"'{item.DrugName}'의 1일 투여량 {daily:0.##}mg이(가) 최대 {max:0.##}mg을 초과합니다.",
                    new[] { item.DrugName });
            }
        }
    }
}
