namespace PharmacyWorkflow.Api.Auth;

/// <summary>시스템에서 사용하는 역할.</summary>
public static class Roles
{
    /// <summary>처방의 (처방 접수)</summary>
    public const string Prescriber = "Prescriber";

    /// <summary>약사 (검수, 조제, 불출 전체)</summary>
    public const string Pharmacist = "Pharmacist";

    /// <summary>조제 보조 (조제 시작/완료만)</summary>
    public const string Assistant = "Assistant";

    /// <summary>조회 전용</summary>
    public const string Viewer = "Viewer";
}

/// <summary>
/// 권한 정책 이름. 조회 API는 별도 정책 없이 "로그인한 모든 역할"에게 열려 있습니다(기본 정책).
/// </summary>
public static class Policies
{
    public const string SubmitPrescription = "SubmitPrescription";
    public const string VerifyPrescription = "VerifyPrescription";
    public const string Dispense = "Dispense";
    public const string ReleasePrescription = "ReleasePrescription";
}
