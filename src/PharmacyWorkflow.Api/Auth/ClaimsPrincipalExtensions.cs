using System.Security.Claims;

namespace PharmacyWorkflow.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// 감사 로그에 남길 처리자 ID. 요청 본문이 아니라 검증된 토큰(sub)에서만 가져오므로 위조할 수 없습니다.
    /// </summary>
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue("sub")
        ?? throw new InvalidOperationException("토큰에 사용자 ID(sub)가 없습니다.");
}
