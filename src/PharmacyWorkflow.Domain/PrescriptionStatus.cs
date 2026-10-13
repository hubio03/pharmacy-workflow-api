namespace PharmacyWorkflow.Domain;

/// <summary>처방의 조제 워크플로우 상태.</summary>
public enum PrescriptionStatus
{
    /// <summary>접수대기</summary>
    Pending,

    /// <summary>검수완료</summary>
    Verified,

    /// <summary>반려 (종료 상태)</summary>
    Rejected,

    /// <summary>조제중</summary>
    InProgress,

    /// <summary>조제완료</summary>
    Completed,

    /// <summary>불출</summary>
    Released
}
