namespace PharmacyWorkflow.Domain.Review;

/// <summary>한 처방에 대한 전체 검수 결과.</summary>
public sealed record ReviewResult(Guid PrescriptionId, IReadOnlyList<ReviewFinding> Findings)
{
    public bool IsClear => Findings.Count == 0;

    public bool HasCritical => Findings.Any(f => f.Severity == ReviewSeverity.Critical);

    public int CriticalCount => Findings.Count(f => f.Severity == ReviewSeverity.Critical);
}
