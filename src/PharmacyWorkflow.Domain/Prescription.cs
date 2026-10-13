namespace PharmacyWorkflow.Domain;

/// <summary>
/// 처방 집합체(Aggregate). 상태 전이 규칙은 컨트롤러가 아니라 이 클래스가 책임집니다.
/// </summary>
public class Prescription
{
    // 허용되는 상태 전이표. 여기에 없는 전이는 모두 차단됩니다.
    private static readonly Dictionary<PrescriptionStatus, PrescriptionStatus[]> AllowedTransitions = new()
    {
        [PrescriptionStatus.Pending]    = new[] { PrescriptionStatus.Verified, PrescriptionStatus.Rejected },
        [PrescriptionStatus.Verified]   = new[] { PrescriptionStatus.InProgress },
        [PrescriptionStatus.InProgress] = new[] { PrescriptionStatus.Completed },
        [PrescriptionStatus.Completed]  = new[] { PrescriptionStatus.Released },
        [PrescriptionStatus.Rejected]   = Array.Empty<PrescriptionStatus>(),
        [PrescriptionStatus.Released]   = Array.Empty<PrescriptionStatus>(),
    };

    private readonly List<PrescriptionItem> _items;
    private readonly List<ReviewLog> _history = new();

    public Guid Id { get; private set; }
    public string PatientId { get; private set; }      // 가상 환자번호 (실제 환자 정보 사용 금지)
    public string PrescriberId { get; private set; }   // 처방의 ID
    public DateTimeOffset PrescribedAt { get; private set; }
    public PrescriptionStatus Status { get; private set; } = PrescriptionStatus.Pending;

    public IReadOnlyCollection<PrescriptionItem> Items => _items;
    public IReadOnlyCollection<ReviewLog> History => _history;

    // EF Core가 DB에서 복원할 때 사용하는 생성자
    private Prescription()
    {
        PatientId = null!;
        PrescriberId = null!;
        _items = new List<PrescriptionItem>();
    }

    public Prescription(string patientId, string prescriberId, IEnumerable<PrescriptionItem> items)
    {
        if (string.IsNullOrWhiteSpace(patientId))
            throw new ArgumentException("환자번호는 필수입니다.", nameof(patientId));
        if (string.IsNullOrWhiteSpace(prescriberId))
            throw new ArgumentException("처방의 ID는 필수입니다.", nameof(prescriberId));

        _items = items?.ToList() ?? throw new ArgumentNullException(nameof(items));
        if (_items.Count == 0)
            throw new ArgumentException("처방 항목은 1개 이상이어야 합니다.", nameof(items));

        Id = Guid.NewGuid();
        PatientId = patientId;
        PrescriberId = prescriberId;
        PrescribedAt = DateTimeOffset.UtcNow;
    }

    public bool CanTransitionTo(PrescriptionStatus next) =>
        AllowedTransitions[Status].Contains(next);

    /// <summary>약사 검수 승인.</summary>
    public void Verify(string pharmacistId, string? note = null) =>
        TransitionTo(PrescriptionStatus.Verified, pharmacistId, note);

    /// <summary>약사 검수 반려. 사유는 필수입니다.</summary>
    public void Reject(string pharmacistId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("반려 사유는 필수입니다.", nameof(reason));

        TransitionTo(PrescriptionStatus.Rejected, pharmacistId, reason);
    }

    public void StartDispensing(string actorId) =>
        TransitionTo(PrescriptionStatus.InProgress, actorId, null);

    public void CompleteDispensing(string actorId) =>
        TransitionTo(PrescriptionStatus.Completed, actorId, null);

    public void Release(string actorId) =>
        TransitionTo(PrescriptionStatus.Released, actorId, null);

    private void TransitionTo(PrescriptionStatus next, string actorId, string? reason)
    {
        if (string.IsNullOrWhiteSpace(actorId))
            throw new ArgumentException("처리자 ID는 필수입니다.", nameof(actorId));

        if (!CanTransitionTo(next))
            throw new InvalidStatusTransitionException(Status, next);

        _history.Add(new ReviewLog(Status, next, actorId, reason, DateTimeOffset.UtcNow));
        Status = next;
    }
}
