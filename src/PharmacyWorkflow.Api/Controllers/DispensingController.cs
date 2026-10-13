using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyWorkflow.Api.Auth;
using PharmacyWorkflow.Api.Contracts;
using PharmacyWorkflow.Api.Services;

namespace PharmacyWorkflow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dispensing")]
public class DispensingController : ControllerBase
{
    private readonly DispensingService _service;

    public DispensingController(DispensingService service) => _service = service;

    /// <summary>검수 대기 목록 (로그인한 모든 역할)</summary>
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<PrescriptionResponse>>> Pending()
    {
        var list = await _service.ListPendingAsync();
        return Ok(list.Select(PrescriptionResponse.FromDomain));
    }

    /// <summary>자동 검수 규칙 결과 조회 (로그인한 모든 역할)</summary>
    [HttpGet("{id:guid}/review-result")]
    public async Task<ActionResult<ReviewResultResponse>> GetReviewResult(Guid id) =>
        ReviewResultResponse.FromDomain(await _service.GetReviewResultAsync(id));

    /// <summary>검수 승인 (약사). 중대 경고가 있으면 note 필수.</summary>
    [HttpPost("{id:guid}/verify")]
    [Authorize(Policy = Policies.VerifyPrescription)]
    public async Task<ActionResult<PrescriptionResponse>> Verify(Guid id, VerifyRequest? request) =>
        PrescriptionResponse.FromDomain(await _service.VerifyAsync(id, User.GetUserId(), request?.Note));

    /// <summary>검수 반려 (약사). 사유 필수.</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = Policies.VerifyPrescription)]
    public async Task<ActionResult<PrescriptionResponse>> Reject(Guid id, RejectRequest request) =>
        PrescriptionResponse.FromDomain(await _service.RejectAsync(id, User.GetUserId(), request.Reason));

    /// <summary>조제 시작 (약사, 조제 보조)</summary>
    [HttpPost("{id:guid}/start")]
    [Authorize(Policy = Policies.Dispense)]
    public async Task<ActionResult<PrescriptionResponse>> Start(Guid id) =>
        PrescriptionResponse.FromDomain(await _service.StartAsync(id, User.GetUserId()));

    /// <summary>조제 완료 (약사, 조제 보조)</summary>
    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = Policies.Dispense)]
    public async Task<ActionResult<PrescriptionResponse>> Complete(Guid id) =>
        PrescriptionResponse.FromDomain(await _service.CompleteAsync(id, User.GetUserId()));

    /// <summary>불출 (약사)</summary>
    [HttpPost("{id:guid}/release")]
    [Authorize(Policy = Policies.ReleasePrescription)]
    public async Task<ActionResult<PrescriptionResponse>> Release(Guid id) =>
        PrescriptionResponse.FromDomain(await _service.ReleaseAsync(id, User.GetUserId()));
}
