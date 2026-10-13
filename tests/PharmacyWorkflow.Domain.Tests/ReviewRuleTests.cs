using PharmacyWorkflow.Domain.Review;

namespace PharmacyWorkflow.Domain.Tests;

public class ReviewRuleTests
{
    private static ReviewResult Review(params PrescriptionItem[] items)
    {
        var prescription = new Prescription("PT-0001", "DOC-01", items);
        return TestReviewer.Create().Review(prescription);
    }

    [Fact]
    public void 문제가_없는_처방은_경고가_없다()
    {
        var result = Review(new PrescriptionItem("샘플정 500mg", 1, 3, 5)); // 1일 1500mg ≤ 3000mg

        Assert.True(result.IsClear);
        Assert.False(result.HasCritical);
    }

    [Fact]
    public void 최대_1일_투여량을_초과하면_중대_경고가_발생한다()
    {
        // 다라정 250mg × 4개 × 3회 = 3000mg > 최대 1500mg
        var result = Review(new PrescriptionItem("다라정 250mg", 4, 3, 5));

        var finding = Assert.Single(result.Findings);
        Assert.Equal("MAX_DAILY_DOSE", finding.RuleCode);
        Assert.Equal(ReviewSeverity.Critical, finding.Severity);
    }

    [Fact]
    public void 최대_1일_투여량과_같으면_경고가_없다()
    {
        // 다라정 250mg × 2개 × 3회 = 1500mg (최대와 동일)
        var result = Review(new PrescriptionItem("다라정 250mg", 2, 3, 5));

        Assert.True(result.IsClear);
    }

    [Fact]
    public void 동일_성분_중복_처방은_주의_경고가_발생한다()
    {
        var result = Review(
            new PrescriptionItem("가나정 500mg", 1, 2, 5),
            new PrescriptionItem("가나플러스정 250mg", 1, 2, 5));

        var finding = Assert.Single(result.Findings);
        Assert.Equal("DUPLICATE_INGREDIENT", finding.RuleCode);
        Assert.Equal(ReviewSeverity.Caution, finding.Severity);
        Assert.Equal(2, finding.DrugNames.Count);
    }

    [Fact]
    public void 병용_금기_조합은_중대_경고가_발생한다()
    {
        var result = Review(
            new PrescriptionItem("가나정 500mg", 1, 2, 5),
            new PrescriptionItem("마바캡슐 100mg", 1, 2, 5));

        var finding = Assert.Single(result.Findings);
        Assert.Equal("DRUG_INTERACTION", finding.RuleCode);
        Assert.Equal(ReviewSeverity.Critical, finding.Severity);
        Assert.True(result.HasCritical);
    }

    [Fact]
    public void 병용_주의_조합은_주의_경고만_발생한다()
    {
        var result = Review(
            new PrescriptionItem("다라정 250mg", 1, 2, 5),
            new PrescriptionItem("마바캡슐 100mg", 1, 2, 5));

        var finding = Assert.Single(result.Findings);
        Assert.Equal(ReviewSeverity.Caution, finding.Severity);
        Assert.False(result.HasCritical);
    }

    [Fact]
    public void 약품_순서가_바뀌어도_같은_병용_금기가_검출된다()
    {
        var result = Review(
            new PrescriptionItem("마바캡슐 100mg", 1, 2, 5),
            new PrescriptionItem("가나정 500mg", 1, 2, 5));

        Assert.Contains(result.Findings, f => f.RuleCode == "DRUG_INTERACTION");
    }

    [Fact]
    public void 약품_정보에_없는_약품은_주의_경고가_발생한다()
    {
        var result = Review(new PrescriptionItem("없는약 100mg", 1, 1, 3));

        var finding = Assert.Single(result.Findings);
        Assert.Equal("UNKNOWN_DRUG", finding.RuleCode);
        Assert.Equal(ReviewSeverity.Caution, finding.Severity);
    }

    [Fact]
    public void 결과는_심각한_경고가_먼저_나온다()
    {
        // 가나정+마바캡슐(중대: 병용 금기) + 없는약(주의: 미등록)
        var result = Review(
            new PrescriptionItem("가나정 500mg", 1, 2, 5),
            new PrescriptionItem("마바캡슐 100mg", 1, 2, 5),
            new PrescriptionItem("없는약 100mg", 1, 1, 3));

        Assert.Equal(2, result.Findings.Count);
        Assert.Equal(ReviewSeverity.Critical, result.Findings[0].Severity);
        Assert.Equal(ReviewSeverity.Caution, result.Findings[1].Severity);
    }
}
