using Microsoft.EntityFrameworkCore;
using PharmacyWorkflow.Api.Data;
using PharmacyWorkflow.Domain;

namespace PharmacyWorkflow.Api.Repositories;

/// <summary>EF Core + SQLite 구현체 (3단계).</summary>
public class EfPrescriptionRepository : IPrescriptionRepository
{
    private readonly PharmacyDbContext _db;

    public EfPrescriptionRepository(PharmacyDbContext db) => _db = db;

    public async Task AddAsync(Prescription prescription)
    {
        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync();
    }

    // 소유 엔티티(Items, History)는 자동으로 함께 로드됩니다. 이후 변경을 추적해야 하므로 추적(tracking)을 유지합니다.
    public Task<Prescription?> GetAsync(Guid id) =>
        _db.Prescriptions.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IReadOnlyList<Prescription>> ListByStatusAsync(PrescriptionStatus status) =>
        await _db.Prescriptions
            .AsNoTracking()
            .Where(p => p.Status == status)
            .OrderBy(p => p.PrescribedAt)
            .ToListAsync();

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
