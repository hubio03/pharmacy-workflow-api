using System.Collections.Concurrent;
using PharmacyWorkflow.Domain;

namespace PharmacyWorkflow.Api.Repositories;

/// <summary>
/// 인메모리 저장소. 3단계부터 앱은 EfPrescriptionRepository를 사용하며,
/// 이 클래스는 DB 없이 서비스 로직만 빠르게 테스트할 때 사용합니다.
/// </summary>
public class InMemoryPrescriptionRepository : IPrescriptionRepository
{
    private readonly ConcurrentDictionary<Guid, Prescription> _store = new();

    public Task AddAsync(Prescription prescription)
    {
        _store[prescription.Id] = prescription;
        return Task.CompletedTask;
    }

    public Task<Prescription?> GetAsync(Guid id)
    {
        _store.TryGetValue(id, out var prescription);
        return Task.FromResult<Prescription?>(prescription);
    }

    public Task<IReadOnlyList<Prescription>> ListByStatusAsync(PrescriptionStatus status)
    {
        IReadOnlyList<Prescription> result = _store.Values
            .Where(p => p.Status == status)
            .OrderBy(p => p.PrescribedAt)
            .ToList();

        return Task.FromResult(result);
    }

    // 객체를 그대로 메모리에 들고 있으므로 따로 저장할 것이 없습니다.
    public Task SaveChangesAsync() => Task.CompletedTask;
}
