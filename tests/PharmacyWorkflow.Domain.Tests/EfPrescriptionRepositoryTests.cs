using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PharmacyWorkflow.Api.Data;
using PharmacyWorkflow.Api.Repositories;
using PharmacyWorkflow.Api.Services;
using PharmacyWorkflow.Domain;

namespace PharmacyWorkflow.Domain.Tests;

/// <summary>SQLite 인메모리 DB로 실제 EF Core 매핑과 트랜잭션 동작을 검증합니다.</summary>
public class EfPrescriptionRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public EfPrescriptionRepositoryTests()
    {
        // 연결을 열어 둔 동안에만 인메모리 DB가 유지되므로, 모든 컨텍스트가 같은 연결을 공유합니다.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var db = NewContext();
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private PharmacyDbContext NewContext() =>
        new(new DbContextOptionsBuilder<PharmacyDbContext>().UseSqlite(_connection).Options);

    private static DispensingService NewService(PharmacyDbContext db) =>
        new(new EfPrescriptionRepository(db), TestReviewer.Create());

    private static PrescriptionItem[] SampleItems() =>
        new[] { new PrescriptionItem("샘플정 500mg", 1, 3, 5) };

    [Fact]
    public async Task 저장한_처방을_새_컨텍스트에서_다시_읽을_수_있다()
    {
        Guid id;
        await using (var db = NewContext())
        {
            var created = await NewService(db).CreateAsync("PT-0001", "DOC-01", SampleItems());
            id = created.Id;
        }

        await using var db2 = NewContext();
        var loaded = await NewService(db2).GetAsync(id);

        Assert.Equal(PrescriptionStatus.Pending, loaded.Status);
        Assert.Equal("PT-0001", loaded.PatientId);
        var item = Assert.Single(loaded.Items);
        Assert.Equal("샘플정 500mg", item.DrugName);
        Assert.Equal(3, item.TimesPerDay);
    }

    [Fact]
    public async Task 상태_변경과_감사_로그가_함께_저장된다()
    {
        Guid id;
        await using (var db = NewContext())
        {
            var service = NewService(db);
            var created = await service.CreateAsync("PT-0001", "DOC-01", SampleItems());
            id = created.Id;

            await service.VerifyAsync(id, "PH-01", "확인 완료");
            await service.StartAsync(id, "PH-02");
        }

        await using var db2 = NewContext();
        var loaded = await NewService(db2).GetAsync(id);

        Assert.Equal(PrescriptionStatus.InProgress, loaded.Status);
        Assert.Equal(
            new[] { PrescriptionStatus.Verified, PrescriptionStatus.InProgress },
            loaded.History.Select(h => h.To).ToArray());
        Assert.Equal("확인 완료", loaded.History.First().Reason);
        Assert.Equal("PH-02", loaded.History.Last().ActorId);
    }

    [Fact]
    public async Task 검수대기_목록은_상태로_필터링된다()
    {
        await using var db = NewContext();
        var service = NewService(db);

        var first = await service.CreateAsync("PT-0001", "DOC-01", SampleItems());
        var second = await service.CreateAsync("PT-0002", "DOC-01", SampleItems());
        await service.VerifyAsync(second.Id, "PH-01", null);

        var pending = await service.ListPendingAsync();

        var only = Assert.Single(pending);
        Assert.Equal(first.Id, only.Id);
    }

    [Fact]
    public async Task 허용되지_않은_전이는_DB에_아무것도_남기지_않는다()
    {
        Guid id;
        await using (var db = NewContext())
        {
            var service = NewService(db);
            var created = await service.CreateAsync("PT-0001", "DOC-01", SampleItems());
            id = created.Id;

            await Assert.ThrowsAsync<InvalidStatusTransitionException>(
                () => service.StartAsync(id, "PH-02"));
        }

        await using var db2 = NewContext();
        var loaded = await NewService(db2).GetAsync(id);

        Assert.Equal(PrescriptionStatus.Pending, loaded.Status);
        Assert.Empty(loaded.History);
    }

    [Fact]
    public async Task 같은_처방을_동시에_변경하면_나중_요청이_충돌로_거부되고_이력도_남지_않는다()
    {
        Guid id;
        await using (var setup = NewContext())
        {
            var created = await NewService(setup).CreateAsync("PT-0001", "DOC-01", SampleItems());
            id = created.Id;
        }

        await using var dbA = NewContext();
        await using var dbB = NewContext();
        var repoA = new EfPrescriptionRepository(dbA);
        var repoB = new EfPrescriptionRepository(dbB);

        // 두 약사가 같은 시점(접수대기)의 처방을 각각 읽는다
        var a = await repoA.GetAsync(id);
        var b = await repoB.GetAsync(id);

        // 약사 A가 먼저 승인하고 저장
        a!.Verify("PH-01");
        await repoA.SaveChangesAsync();

        // 약사 B는 오래된 상태를 기준으로 반려를 시도 → 저장 시 충돌
        b!.Reject("PH-02", "병용 금기 의심");
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => repoB.SaveChangesAsync());

        await using var verifyDb = NewContext();
        var loaded = await NewService(verifyDb).GetAsync(id);

        Assert.Equal(PrescriptionStatus.Verified, loaded.Status);
        var log = Assert.Single(loaded.History);   // B의 반려 이력은 롤백되어 남지 않음
        Assert.Equal("PH-01", log.ActorId);
    }
}
