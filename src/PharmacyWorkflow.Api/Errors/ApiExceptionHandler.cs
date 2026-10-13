using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyWorkflow.Api.Auth;
using PharmacyWorkflow.Domain;
using PharmacyWorkflow.Domain.Review;

namespace PharmacyWorkflow.Api.Errors;

/// <summary>
/// 도메인/서비스 예외를 표준 오류 응답(ProblemDetails + code + traceId)으로 변환합니다.
/// 응답 작성은 IProblemDetailsService를 거치므로 401/403/404/400 등 다른 오류와 같은 형식이 됩니다.
/// </summary>
public class ApiExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;

    public ApiExceptionHandler(IProblemDetailsService problemDetails) => _problemDetails = problemDetails;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail, code) = exception switch
        {
            AuthenticationFailedException =>
                (StatusCodes.Status401Unauthorized, "로그인 실패", exception.Message, "INVALID_CREDENTIALS"),
            InvalidStatusTransitionException =>
                (StatusCodes.Status409Conflict, "허용되지 않은 상태 변경", exception.Message, "INVALID_STATUS_TRANSITION"),
            DbUpdateConcurrencyException =>
                (StatusCodes.Status409Conflict, "동시 수정 충돌",
                 "다른 사용자가 먼저 이 처방을 변경했습니다. 다시 조회한 뒤 시도해 주세요.", "CONCURRENCY_CONFLICT"),
            ReviewOverrideRequiredException =>
                (StatusCodes.Status422UnprocessableEntity, "중대 경고 확인 사유 필요", exception.Message, "REVIEW_OVERRIDE_REQUIRED"),
            KeyNotFoundException =>
                (StatusCodes.Status404NotFound, "리소스를 찾을 수 없음", exception.Message, "NOT_FOUND"),
            ArgumentException =>
                (StatusCodes.Status400BadRequest, "잘못된 요청", exception.Message, "BAD_REQUEST"),
            _ => (0, string.Empty, string.Empty, string.Empty)
        };

        if (status == 0)
            return false; // 처리하지 않은 예외는 기본 500 처리로 넘김

        httpContext.Response.StatusCode = status;

        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;

        var written = await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });

        if (!written)
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
