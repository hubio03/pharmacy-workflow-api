using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PharmacyWorkflow.Domain.Tests;

/// <summary>워크플로우 전체를 HTTP 수준에서 검증하는 통합 테스트.</summary>
[Collection(ApiCollection.Name)]
public class ApiWorkflowTests
{
    private readonly ApiFactory _factory;

    public ApiWorkflowTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task 검수_대기_목록에는_접수대기_처방만_나타난다()
    {
        var doctor = await _factory.LoginAsync("doctor01");
        var pharmacist = await _factory.LoginAsync("pharm01");
        var viewer = await _factory.LoginAsync("viewer01");

        var waiting = await doctor.CreatePrescriptionAsync(ApiTestHelpers.SamplePrescription("PT-W1"));
        var verified = await doctor.CreatePrescriptionAsync(ApiTestHelpers.SamplePrescription("PT-W2"));
        await pharmacist.PostAsJsonAsync($"/api/dispensing/{verified}/verify", new { });

        var pending = await viewer.GetFromJsonAsync<JsonElement>("/api/dispensing/pending");
        var ids = pending.EnumerateArray().Select(e => e.GetProperty("id").GetGuid()).ToList();

        Assert.Contains(waiting, ids);
        Assert.DoesNotContain(verified, ids);
    }

    [Fact]
    public async Task 조제_순서를_어기면_409와_상태_전이_오류_코드가_반환된다()
    {
        var doctor = await _factory.LoginAsync("doctor01");
        var assistant = await _factory.LoginAsync("assist01");
        var id = await doctor.CreatePrescriptionAsync(ApiTestHelpers.SamplePrescription());

        // 검수 전에 조제를 시작하려는 경우
        var response = await assistant.PostAsync($"/api/dispensing/{id}/start", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("INVALID_STATUS_TRANSITION", await response.ErrorCodeAsync());
    }

    [Fact]
    public async Task 반려는_사유가_필요하고_반려된_처방은_이후_진행할_수_없다()
    {
        var doctor = await _factory.LoginAsync("doctor01");
        var pharmacist = await _factory.LoginAsync("pharm01");
        var id = await doctor.CreatePrescriptionAsync(ApiTestHelpers.SamplePrescription());

        var blank = await pharmacist.PostAsJsonAsync($"/api/dispensing/{id}/reject", new { reason = "" });
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
        Assert.Equal("BAD_REQUEST", await blank.ErrorCodeAsync());

        var rejected = await pharmacist.PostAsJsonAsync(
            $"/api/dispensing/{id}/reject", new { reason = "용량 재확인 필요" });
        Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
        Assert.Equal("Rejected", (await rejected.ReadJsonAsync()).GetProperty("status").GetString());

        var verifyAgain = await pharmacist.PostAsJsonAsync($"/api/dispensing/{id}/verify", new { });
        Assert.Equal(HttpStatusCode.Conflict, verifyAgain.StatusCode);
    }

    [Fact]
    public async Task 요청_본문의_필수_값이_빠지면_400과_VALIDATION_ERROR가_반환된다()
    {
        var doctor = await _factory.LoginAsync("doctor01");
        var pharmacist = await _factory.LoginAsync("pharm01");
        var id = await doctor.CreatePrescriptionAsync(ApiTestHelpers.SamplePrescription());

        var response = await pharmacist.PostAsJsonAsync($"/api/dispensing/{id}/reject", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", await response.ErrorCodeAsync());
    }

    [Fact]
    public async Task 처방_항목이_잘못되면_400이_반환된다()
    {
        var doctor = await _factory.LoginAsync("doctor01");

        var noItems = await doctor.PostAsJsonAsync("/api/prescriptions", new
        {
            patientId = "PT-0001",
            prescriberId = "DOC-01",
            items = Array.Empty<object>(),
        });

        var zeroDose = await doctor.PostAsJsonAsync("/api/prescriptions", new
        {
            patientId = "PT-0001",
            prescriberId = "DOC-01",
            items = new[] { new { drugName = "샘플정 500mg", dosePerTime = 0, timesPerDay = 3, days = 5 } },
        });

        Assert.Equal(HttpStatusCode.BadRequest, noItems.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, zeroDose.StatusCode);
    }

    [Fact]
    public async Task 검수_결과를_HTTP로_조회할_수_있다()
    {
        var doctor = await _factory.LoginAsync("doctor01");
        var viewer = await _factory.LoginAsync("viewer01");
        var id = await doctor.CreatePrescriptionAsync(ApiTestHelpers.CriticalPrescription());

        var result = await viewer.GetFromJsonAsync<JsonElement>($"/api/dispensing/{id}/review-result");

        Assert.True(result.GetProperty("hasCritical").GetBoolean());
        var finding = result.GetProperty("findings")[0];
        Assert.Equal("DRUG_INTERACTION", finding.GetProperty("ruleCode").GetString());
        Assert.Equal("Critical", finding.GetProperty("severity").GetString());
    }

    [Fact]
    public async Task 중대_경고_승인_사유가_감사_로그에_기록된다()
    {
        var doctor = await _factory.LoginAsync("doctor01");
        var pharmacist = await _factory.LoginAsync("pharm01");
        var id = await doctor.CreatePrescriptionAsync(ApiTestHelpers.CriticalPrescription());

        await pharmacist.PostAsJsonAsync(
            $"/api/dispensing/{id}/verify", new { note = "처방의 전화 확인 완료" });

        var history = await pharmacist.GetFromJsonAsync<JsonElement>($"/api/prescriptions/{id}/history");
        var log = Assert.Single(history.EnumerateArray());

        Assert.Equal("pharm01", log.GetProperty("actorId").GetString());
        Assert.Equal("처방의 전화 확인 완료", log.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task Swagger_문서는_로그인_없이_열람할_수_있다()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/auth/login", text);
        Assert.Contains("/api/dispensing/{id}/review-result", text);
    }
}
