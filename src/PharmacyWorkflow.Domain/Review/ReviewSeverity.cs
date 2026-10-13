namespace PharmacyWorkflow.Domain.Review;

/// <summary>검수 경고 수준. 값이 클수록 심각합니다.</summary>
public enum ReviewSeverity
{
    /// <summary>참고</summary>
    Info = 0,

    /// <summary>주의</summary>
    Caution = 1,

    /// <summary>중대 (승인하려면 확인 사유 필요)</summary>
    Critical = 2
}
