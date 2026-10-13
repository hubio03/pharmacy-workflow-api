using PharmacyWorkflow.Domain;

namespace PharmacyWorkflow.Api.Repositories;

public interface IPrescriptionRepository
{
    /// <summary>새 처방을 저장합니다.</summary>
    Task AddAsync(Prescription prescription);

    Task<Prescription?> GetAsync(Guid id);

    Task<IReadOnlyList<Prescription>> ListByStatusAsync(PrescriptionStatus status);

    /// <summary>
    /// 조회한 처방에 가한 변경(상태 + 이력 추가)을 한 번에 저장합니다.
    /// EF Core 구현체에서는 하나의 DB 트랜잭션으로 처리되어 둘 중 하나만 저장되는 일이 없습니다.
    /// </summary>
    Task SaveChangesAsync();
}
