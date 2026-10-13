using PharmacyWorkflow.Domain;
using PharmacyWorkflow.Domain.Review;

namespace PharmacyWorkflow.Api.Contracts;

public record PrescriptionItemResponse(string DrugName, decimal DosePerTime, int TimesPerDay, int Days);

public record PrescriptionResponse(
    Guid Id,
    string PatientId,
    string PrescriberId,
    DateTimeOffset PrescribedAt,
    PrescriptionStatus Status,
    IReadOnlyList<PrescriptionItemResponse> Items)
{
    public static PrescriptionResponse FromDomain(Prescription p) => new(
        p.Id,
        p.PatientId,
        p.PrescriberId,
        p.PrescribedAt,
        p.Status,
        p.Items.Select(i => new PrescriptionItemResponse(i.DrugName, i.DosePerTime, i.TimesPerDay, i.Days)).ToList());
}

public record ReviewLogResponse(
    PrescriptionStatus From,
    PrescriptionStatus To,
    string ActorId,
    string? Reason,
    DateTimeOffset At)
{
    public static ReviewLogResponse FromDomain(ReviewLog log) =>
        new(log.From, log.To, log.ActorId, log.Reason, log.At);
}

public record ReviewFindingResponse(
    string RuleCode,
    ReviewSeverity Severity,
    string Message,
    IReadOnlyList<string> DrugNames);

public record ReviewResultResponse(
    Guid PrescriptionId,
    bool IsClear,
    bool HasCritical,
    IReadOnlyList<ReviewFindingResponse> Findings)
{
    public static ReviewResultResponse FromDomain(ReviewResult result) => new(
        result.PrescriptionId,
        result.IsClear,
        result.HasCritical,
        result.Findings
            .Select(f => new ReviewFindingResponse(f.RuleCode, f.Severity, f.Message, f.DrugNames))
            .ToList());
}
