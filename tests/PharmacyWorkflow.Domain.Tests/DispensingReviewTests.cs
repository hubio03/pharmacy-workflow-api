using PharmacyWorkflow.Api.Repositories;
using PharmacyWorkflow.Api.Services;
using PharmacyWorkflow.Domain.Review;

namespace PharmacyWorkflow.Domain.Tests;

public class DispensingReviewTests
{
    private static DispensingService CreateService() =>
        new(new InMemoryPrescriptionRepository(), TestReviewer.Create());

    // 가나돌 + 마바신 = 병용 금기(중대)
    private static PrescriptionItem[] CriticalItems() => new[]
    {
        new PrescriptionItem("가나정 500mg", 1, 2, 5),
        new PrescriptionItem("마바캡슐 100mg", 1, 2, 5),
    };

    // 다라민 + 마바신 = 병용 주의(주의 수준)
    private static PrescriptionItem[] CautionItems() => new[]
    {
        new PrescriptionItem("다라정 250mg", 1, 2, 5),
        new PrescriptionItem("마바캡슐 100mg", 1, 2, 5),
    };

    [Fact]
    public async Task 검수_결과_조회는_상태를_바꾸지_않는다()
    {
        var service = CreateService();
        var created = await service.CreateAsync("PT-0001", "DOC-01", CriticalItems());

        var result = await service.GetReviewResultAsync(created.Id);

        Assert.True(result.HasCritical);
        Assert.Equal(PrescriptionStatus.Pending, (await service.GetAsync(created.Id)).Status);
    }

    [Fact]
    public async Task 중대_경고가_있으면_사유_없이_승인할_수_없다()
    {
        var service = CreateService();
        var created = await service.CreateAsync("PT-0001", "DOC-01", CriticalItems());

        await Assert.ThrowsAsync<ReviewOverrideRequiredException>(
            () => service.VerifyAsync(created.Id, "PH-01", null));

        var loaded = await service.GetAsync(created.Id);
        Assert.Equal(PrescriptionStatus.Pending, loaded.Status);
        Assert.Empty(loaded.History);
    }

    [Fact]
    public async Task 공백_사유도_사유로_인정되지_않는다()
    {
        var service = CreateService();
        var created = await service.CreateAsync("PT-0001", "DOC-01", CriticalItems());

        await Assert.ThrowsAsync<ReviewOverrideRequiredException>(
            () => service.VerifyAsync(created.Id, "PH-01", "   "));
    }

    [Fact]
    public async Task 중대_경고가_있어도_사유를_적으면_승인되고_감사_로그에_사유가_남는다()
    {
        var service = CreateService();
        var created = await service.CreateAsync("PT-0001", "DOC-01", CriticalItems());

        var verified = await service.VerifyAsync(created.Id, "PH-01", "처방의 확인 완료 (전화 확인)");

        Assert.Equal(PrescriptionStatus.Verified, verified.Status);
        var log = Assert.Single(verified.History);
        Assert.Equal("처방의 확인 완료 (전화 확인)", log.Reason);
    }

    [Fact]
    public async Task 주의_수준_경고만_있으면_사유_없이_승인할_수_있다()
    {
        var service = CreateService();
        var created = await service.CreateAsync("PT-0001", "DOC-01", CautionItems());

        var verified = await service.VerifyAsync(created.Id, "PH-01", null);

        Assert.Equal(PrescriptionStatus.Verified, verified.Status);
    }

    [Fact]
    public async Task 이미_승인된_처방은_사유_검사보다_상태_전이_오류가_먼저_발생한다()
    {
        var service = CreateService();
        var created = await service.CreateAsync("PT-0001", "DOC-01", CriticalItems());
        await service.VerifyAsync(created.Id, "PH-01", "확인 완료");

        await Assert.ThrowsAsync<InvalidStatusTransitionException>(
            () => service.VerifyAsync(created.Id, "PH-01", null));
    }
}
