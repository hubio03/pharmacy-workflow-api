using PharmacyWorkflow.Domain;

namespace PharmacyWorkflow.Domain.Tests;

public class PrescriptionTests
{
    private static Prescription CreateSample() =>
        new("PT-0001", "DOC-01", new[]
        {
            new PrescriptionItem("샘플정 500mg", 1, 3, 5)
        });

    // ---------- 생성 ----------

    [Fact]
    public void 새_처방은_접수대기_상태로_시작한다()
    {
        var p = CreateSample();

        Assert.Equal(PrescriptionStatus.Pending, p.Status);
        Assert.Empty(p.History);
    }

    [Fact]
    public void 처방_항목이_없으면_생성할_수_없다()
    {
        Assert.Throws<ArgumentException>(() =>
            new Prescription("PT-0001", "DOC-01", Array.Empty<PrescriptionItem>()));
    }

    // ---------- 정상 흐름 ----------

    [Fact]
    public void 정상_흐름은_접수대기에서_불출까지_진행된다()
    {
        var p = CreateSample();

        p.Verify("PH-01");
        p.StartDispensing("PH-02");
        p.CompleteDispensing("PH-02");
        p.Release("PH-01");

        Assert.Equal(PrescriptionStatus.Released, p.Status);
        Assert.Equal(4, p.History.Count);
    }

    [Fact]
    public void 상태_변경_이력에_처리자와_사유가_기록된다()
    {
        var p = CreateSample();

        p.Reject("PH-01", "용량 확인 필요");

        var log = Assert.Single(p.History);
        Assert.Equal(PrescriptionStatus.Pending, log.From);
        Assert.Equal(PrescriptionStatus.Rejected, log.To);
        Assert.Equal("PH-01", log.ActorId);
        Assert.Equal("용량 확인 필요", log.Reason);
    }

    // ---------- 차단되어야 하는 전이 ----------

    [Fact]
    public void 검수_전에는_조제를_시작할_수_없다()
    {
        var p = CreateSample();

        Assert.Throws<InvalidStatusTransitionException>(() => p.StartDispensing("PH-02"));
        Assert.Equal(PrescriptionStatus.Pending, p.Status); // 실패 시 상태 불변
        Assert.Empty(p.History);                            // 실패 시 이력도 남지 않음
    }

    [Fact]
    public void 반려된_처방은_다시_검수할_수_없다()
    {
        var p = CreateSample();
        p.Reject("PH-01", "병용 금기 의심");

        Assert.Throws<InvalidStatusTransitionException>(() => p.Verify("PH-01"));
    }

    [Fact]
    public void 반려는_사유가_없으면_할_수_없다()
    {
        var p = CreateSample();

        Assert.Throws<ArgumentException>(() => p.Reject("PH-01", " "));
        Assert.Equal(PrescriptionStatus.Pending, p.Status);
    }

    [Fact]
    public void 불출된_처방은_더_이상_변경할_수_없다()
    {
        var p = CreateSample();
        p.Verify("PH-01");
        p.StartDispensing("PH-02");
        p.CompleteDispensing("PH-02");
        p.Release("PH-01");

        Assert.Throws<InvalidStatusTransitionException>(() => p.CompleteDispensing("PH-02"));
    }

    // ---------- 전이표 전체 검증 ----------

    [Theory]
    [InlineData(PrescriptionStatus.Pending, PrescriptionStatus.Verified, true)]
    [InlineData(PrescriptionStatus.Pending, PrescriptionStatus.Rejected, true)]
    [InlineData(PrescriptionStatus.Pending, PrescriptionStatus.InProgress, false)]
    [InlineData(PrescriptionStatus.Pending, PrescriptionStatus.Completed, false)]
    [InlineData(PrescriptionStatus.Pending, PrescriptionStatus.Released, false)]
    public void 접수대기_상태의_전이_가능_여부(
        PrescriptionStatus from, PrescriptionStatus to, bool expected)
    {
        var p = CreateSample();
        Assert.Equal(from, p.Status);

        Assert.Equal(expected, p.CanTransitionTo(to));
    }
}
