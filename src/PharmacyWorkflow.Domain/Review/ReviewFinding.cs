namespace PharmacyWorkflow.Domain.Review;

/// <summary>규칙 하나가 찾아낸 검수 결과 한 건.</summary>
public sealed record ReviewFinding(
    string RuleCode,
    ReviewSeverity Severity,
    string Message,
    IReadOnlyList<string> DrugNames);
