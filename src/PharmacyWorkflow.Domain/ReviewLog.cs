namespace PharmacyWorkflow.Domain;

/// <summary>상태 변경 감사 로그 (누가, 언제, 어떤 상태로, 왜).</summary>
public sealed class ReviewLog
{
    public PrescriptionStatus From { get; private set; }
    public PrescriptionStatus To { get; private set; }
    public string ActorId { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset At { get; private set; }

    // EF Core가 DB에서 복원할 때 사용하는 생성자
    private ReviewLog() => ActorId = null!;

    public ReviewLog(
        PrescriptionStatus fromStatus,
        PrescriptionStatus toStatus,
        string actorId,
        string? reason,
        DateTimeOffset at)
    {
        From = fromStatus;
        To = toStatus;
        ActorId = actorId;
        Reason = reason;
        At = at;
    }
}
