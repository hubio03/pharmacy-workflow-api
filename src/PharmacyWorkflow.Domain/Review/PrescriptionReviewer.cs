namespace PharmacyWorkflow.Domain.Review;

/// <summary>등록된 모든 규칙을 실행해 검수 결과를 모읍니다.</summary>
public sealed class PrescriptionReviewer
{
    private readonly IReadOnlyList<IReviewRule> _rules;

    public PrescriptionReviewer(IEnumerable<IReviewRule> rules) => _rules = rules.ToList();

    public ReviewResult Review(Prescription prescription)
    {
        var findings = _rules
            .SelectMany(rule => rule.Evaluate(prescription))
            .OrderByDescending(f => f.Severity)               // 심각한 경고가 먼저
            .ThenBy(f => f.RuleCode, StringComparer.Ordinal)
            .ToList();

        return new ReviewResult(prescription.Id, findings);
    }
}
