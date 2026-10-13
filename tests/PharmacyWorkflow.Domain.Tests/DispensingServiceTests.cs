using PharmacyWorkflow.Api.Repositories;
using PharmacyWorkflow.Api.Services;
using PharmacyWorkflow.Domain;

namespace PharmacyWorkflow.Domain.Tests;

public class DispensingServiceTests
{
    private static DispensingService CreateService() =>
        new(new InMemoryPrescriptionRepository(), TestReviewer.Create());

    private static PrescriptionItem[] SampleItems() =>
        new[] { new PrescriptionItem("샘플정 500mg", 1, 3, 5) };

    [Fact]
    public async Task 접수한_처방은_검수대기_목록에_나타난다()
    {
        var service = CreateService();

        var created = await service.CreateAsync("PT-0001", "DOC-01", SampleItems());
        var pending = await service.ListPendingAsync();

        Assert.Contains(pending, p => p.Id == created.Id);
    }

    [Fact]
    public async Task 검수_승인하면_검수대기_목록에서_사라진다()
    {
        var service = CreateService();
        var created = await service.CreateAsync("PT-0001", "DOC-01", SampleItems());

        await service.VerifyAsync(created.Id, "PH-01", null);
        var pending = await service.ListPendingAsync();

        Assert.DoesNotContain(pending, p => p.Id == created.Id);
    }

    [Fact]
    public async Task 존재하지_않는_처방을_조회하면_KeyNotFoundException이_발생한다()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task 검수_전에_조제를_시작하면_InvalidStatusTransitionException이_발생한다()
    {
        var service = CreateService();
        var created = await service.CreateAsync("PT-0001", "DOC-01", SampleItems());

        await Assert.ThrowsAsync<InvalidStatusTransitionException>(
            () => service.StartAsync(created.Id, "PH-02"));
    }
}
