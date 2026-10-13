using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PharmacyWorkflow.Api.Auth;

namespace PharmacyWorkflow.Domain.Tests;

/// <summary>API 통합 테스트에서 공통으로 쓰는 도우미.</summary>
internal static class ApiTestHelpers
{
    /// <summary>데모 계정으로 로그인해 토큰이 설정된 HttpClient를 만듭니다.</summary>
    public static async Task<HttpClient> LoginAsync(this ApiFactory factory, string userId)
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new { userId, password = InMemoryUserStore.DemoPassword });
        response.EnsureSuccessStatusCode();

        var body = await response.ReadJsonAsync();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());

        return client;
    }

    // 문제가 없는 처방 (샘플정: 1일 1500mg ≤ 최대 3000mg)
    public static object SamplePrescription(string patientId = "PT-0001") => new
    {
        patientId,
        prescriberId = "DOC-01",
        items = new[] { new { drugName = "샘플정 500mg", dosePerTime = 1, timesPerDay = 3, days = 5 } },
    };

    // 가나돌 + 마바신 = 가상의 병용 금기(중대 경고)
    public static object CriticalPrescription(string patientId = "PT-0002") => new
    {
        patientId,
        prescriberId = "DOC-01",
        items = new[]
        {
            new { drugName = "가나정 500mg", dosePerTime = 1, timesPerDay = 2, days = 5 },
            new { drugName = "마바캡슐 100mg", dosePerTime = 1, timesPerDay = 2, days = 5 },
        },
    };

    /// <summary>처방을 접수하고(201 확인) 생성된 처방 ID를 반환합니다.</summary>
    public static async Task<Guid> CreatePrescriptionAsync(this HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/api/prescriptions", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var json = await response.ReadJsonAsync();
        return json.GetProperty("id").GetGuid();
    }

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    /// <summary>표준 오류 응답의 code 값을 읽습니다.</summary>
    public static async Task<string?> ErrorCodeAsync(this HttpResponseMessage response) =>
        (await response.ReadJsonAsync()).GetProperty("code").GetString();
}
