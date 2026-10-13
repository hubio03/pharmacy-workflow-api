using System.Net;
using System.Net.Http.Json;
using PharmacyWorkflow.Api.Auth;

namespace PharmacyWorkflow.Domain.Tests;

/// <summary>로그인, 역할별 권한, 표준 오류 응답을 실제 HTTP 요청으로 검증합니다.</summary>
[Collection(ApiCollection.Name)]
public class ApiAuthorizationTests
{
    private readonly ApiFactory _factory;

    public ApiAuthorizationTests(ApiFactory factory) => _factory = factory;

    // ---------- 로그인 ----------

    [Fact]
    public async Task 로그인하면_토큰과_역할이_반환된다()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new { userId = "pharm01", password = InMemoryUserStore.DemoPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.ReadJsonAsync();
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("accessToken").GetString()));
        Assert.Equal("Pharmacist", json.GetProperty("role").GetString());
    }

    [Fact]
    public async Task 비밀번호가_틀리면_401과_표준_오류_코드가_반환된다()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new { userId = "pharm01", password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("INVALID_CREDENTIALS", await response.ErrorCodeAsync());
    }

    // ---------- 인증 / 권한 ----------

    [Fact]
    public async Task 토큰_없이_호출하면_401이다()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/dispensing/pending");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("UNAUTHORIZED", await response.ErrorCodeAsync());
    }

    [Fact]
    public async Task 조회_전용_계정은_조회할_수_있지만_처방_접수는_403이다()
    {
        var viewer = await _factory.LoginAsync("viewer01");

        var list = await viewer.GetAsync("/api/dispensing/pending");
        var create = await viewer.PostAsJsonAsync("/api/prescriptions", ApiTestHelpers.SamplePrescription());

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal("FORBIDDEN", await create.ErrorCodeAsync());
    }

    [Fact]
    public async Task 조제_보조는_검수_승인을_할_수_없다()
    {
        var assistant = await _factory.LoginAsync("assist01");

        // 존재하지 않는 처방이어도 권한 검사가 먼저이므로 404가 아니라 403이 나옵니다.
        var response = await assistant.PostAsJsonAsync($"/api/dispensing/{Guid.NewGuid()}/verify", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- 전체 흐름 ----------

    [Fact]
    public async Task 접수에서_불출까지_역할별로_진행되고_감사_로그에는_토큰의_사용자가_기록된다()
    {
        var doctor = await _factory.LoginAsync("doctor01");
        var pharmacist = await _factory.LoginAsync("pharm01");
        var assistant = await _factory.LoginAsync("assist01");

        var id = await doctor.CreatePrescriptionAsync(ApiTestHelpers.SamplePrescription());

        Assert.Equal(HttpStatusCode.OK,
            (await pharmacist.PostAsJsonAsync($"/api/dispensing/{id}/verify", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await assistant.PostAsync($"/api/dispensing/{id}/start", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await assistant.PostAsync($"/api/dispensing/{id}/complete", null)).StatusCode);

        // 조제 보조는 불출할 수 없고, 약사만 가능
        Assert.Equal(HttpStatusCode.Forbidden,
            (await assistant.PostAsync($"/api/dispensing/{id}/release", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await pharmacist.PostAsync($"/api/dispensing/{id}/release", null)).StatusCode);

        var history = await pharmacist.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/prescriptions/{id}/history");
        var actors = history.EnumerateArray().Select(e => e.GetProperty("actorId").GetString()).ToArray();

        Assert.Equal(new[] { "pharm01", "assist01", "assist01", "pharm01" }, actors);
    }

    // ---------- 표준 오류 응답 ----------

    [Fact]
    public async Task 중대_경고_처방은_사유_없이_승인하면_422이고_사유를_적으면_승인된다()
    {
        var doctor = await _factory.LoginAsync("doctor01");
        var pharmacist = await _factory.LoginAsync("pharm01");
        var id = await doctor.CreatePrescriptionAsync(ApiTestHelpers.CriticalPrescription());

        var rejected = await pharmacist.PostAsJsonAsync($"/api/dispensing/{id}/verify", new { });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, rejected.StatusCode);
        Assert.Equal("REVIEW_OVERRIDE_REQUIRED", await rejected.ErrorCodeAsync());

        var approved = await pharmacist.PostAsJsonAsync(
            $"/api/dispensing/{id}/verify", new { note = "처방의 전화 확인 완료" });

        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
    }

    [Fact]
    public async Task 오류_응답은_code와_traceId를_포함한다()
    {
        var pharmacist = await _factory.LoginAsync("pharm01");

        var response = await pharmacist.GetAsync($"/api/prescriptions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var json = await response.ReadJsonAsync();
        Assert.Equal("NOT_FOUND", json.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("traceId").GetString()));
        Assert.Equal(404, json.GetProperty("status").GetInt32());
    }
}
