namespace PharmacyWorkflow.Api.Contracts;

public record PrescriptionItemRequest(string DrugName, decimal DosePerTime, int TimesPerDay, int Days);

public record CreatePrescriptionRequest(
    string PatientId,
    string PrescriberId,
    List<PrescriptionItemRequest> Items);

/// <summary>검수 승인 요청. 처리자는 토큰에서 가져오므로 본문에 없습니다. 본문 자체를 생략해도 됩니다.</summary>
public record VerifyRequest(string? Note);

/// <summary>검수 반려 요청. 처리자는 토큰에서 가져옵니다.</summary>
public record RejectRequest(string Reason);

public record LoginRequest(string UserId, string Password);

public record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, string UserId, string Role);
