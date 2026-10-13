using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyWorkflow.Api.Auth;
using PharmacyWorkflow.Api.Contracts;
using PharmacyWorkflow.Api.Services;
using PharmacyWorkflow.Domain;

namespace PharmacyWorkflow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/prescriptions")]
public class PrescriptionsController : ControllerBase
{
    private readonly DispensingService _service;

    public PrescriptionsController(DispensingService service) => _service = service;

    /// <summary>처방 접수 (처방의, 약사)</summary>
    [HttpPost]
    [Authorize(Policy = Policies.SubmitPrescription)]
    public async Task<ActionResult<PrescriptionResponse>> Create(CreatePrescriptionRequest request)
    {
        var items = request.Items
            .Select(i => new PrescriptionItem(i.DrugName, i.DosePerTime, i.TimesPerDay, i.Days));

        var prescription = await _service.CreateAsync(request.PatientId, request.PrescriberId, items);

        return CreatedAtAction(
            nameof(Get), new { id = prescription.Id }, PrescriptionResponse.FromDomain(prescription));
    }

    /// <summary>처방 상세 조회 (로그인한 모든 역할)</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PrescriptionResponse>> Get(Guid id) =>
        PrescriptionResponse.FromDomain(await _service.GetAsync(id));

    /// <summary>상태 변경 이력 조회 (로그인한 모든 역할)</summary>
    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<IEnumerable<ReviewLogResponse>>> History(Guid id)
    {
        var prescription = await _service.GetAsync(id);
        return Ok(prescription.History.Select(ReviewLogResponse.FromDomain));
    }
}
