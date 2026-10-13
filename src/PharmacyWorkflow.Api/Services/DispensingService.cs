using PharmacyWorkflow.Api.Repositories;
using PharmacyWorkflow.Domain;
using PharmacyWorkflow.Domain.Review;

namespace PharmacyWorkflow.Api.Services;

/// <summary>
/// 유스케이스 조율 계층. 상태 전이 규칙은 도메인(Prescription)이, 자동 검수는 PrescriptionReviewer가 담당하고,
/// 이 서비스는 "조회 → 검수/도메인 호출 → 저장"만 맡습니다.
/// </summary>
public class DispensingService
{
    private readonly IPrescriptionRepository _repository;
    private readonly PrescriptionReviewer _reviewer;

    public DispensingService(IPrescriptionRepository repository, PrescriptionReviewer reviewer)
    {
        _repository = repository;
        _reviewer = reviewer;
    }

    public async Task<Prescription> CreateAsync(
        string patientId, string prescriberId, IEnumerable<PrescriptionItem> items)
    {
        var prescription = new Prescription(patientId, prescriberId, items);
        await _repository.AddAsync(prescription);
        return prescription;
    }

    public async Task<Prescription> GetAsync(Guid id) =>
        await _repository.GetAsync(id)
        ?? throw new KeyNotFoundException($"처방 '{id}'을(를) 찾을 수 없습니다.");

    public Task<IReadOnlyList<Prescription>> ListPendingAsync() =>
        _repository.ListByStatusAsync(PrescriptionStatus.Pending);

    /// <summary>자동 검수 규칙 결과 조회 (상태를 바꾸지 않음)</summary>
    public async Task<ReviewResult> GetReviewResultAsync(Guid id) =>
        _reviewer.Review(await GetAsync(id));

    /// <summary>
    /// 검수 승인. 중대(Critical) 경고가 있는 처방은 확인 사유(note) 없이는 승인할 수 없고,
    /// 입력한 사유는 감사 로그에 그대로 남습니다.
    /// </summary>
    public async Task<Prescription> VerifyAsync(Guid id, string pharmacistId, string? note)
    {
        var prescription = await GetAsync(id);

        // 이미 승인할 수 없는 상태라면 사유 검사보다 상태 전이 오류가 먼저 나도록 순서를 둡니다.
        if (prescription.CanTransitionTo(PrescriptionStatus.Verified) && string.IsNullOrWhiteSpace(note))
        {
            var review = _reviewer.Review(prescription);
            if (review.HasCritical)
                throw new ReviewOverrideRequiredException(review);
        }

        prescription.Verify(pharmacistId, note);
        await _repository.SaveChangesAsync();
        return prescription;
    }

    public Task<Prescription> RejectAsync(Guid id, string pharmacistId, string reason) =>
        ApplyAsync(id, p => p.Reject(pharmacistId, reason));

    public Task<Prescription> StartAsync(Guid id, string actorId) =>
        ApplyAsync(id, p => p.StartDispensing(actorId));

    public Task<Prescription> CompleteAsync(Guid id, string actorId) =>
        ApplyAsync(id, p => p.CompleteDispensing(actorId));

    public Task<Prescription> ReleaseAsync(Guid id, string actorId) =>
        ApplyAsync(id, p => p.Release(actorId));

    private async Task<Prescription> ApplyAsync(Guid id, Action<Prescription> action)
    {
        var prescription = await GetAsync(id);

        action(prescription);                  // 허용되지 않은 전이면 여기서 예외 → 저장되지 않음
        await _repository.SaveChangesAsync();  // 상태 변경 + 이력 추가를 한 트랜잭션으로 저장

        return prescription;
    }
}
