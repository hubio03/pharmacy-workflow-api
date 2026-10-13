using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace PharmacyWorkflow.Domain.Tests;

/// <summary>실제 앱(Program)을 테스트 서버로 띄웁니다. DB는 새 임시 파일을 사용합니다.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"pharmacy-test-{Guid.NewGuid():N}.db");

    public ApiFactory()
    {
        // 환경 변수는 앱이 시작될 때 가장 먼저 읽히므로 연결 문자열을 안정적으로 바꿀 수 있습니다.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", $"Data Source={_dbPath}");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        try
        {
            SqliteConnection.ClearAllPools();
            File.Delete(_dbPath);
        }
        catch (IOException)
        {
            // 임시 파일 정리에 실패해도 테스트 결과에는 영향이 없습니다.
        }
    }
}

/// <summary>
/// API 통합 테스트 클래스들이 앱 인스턴스(와 DB)를 하나만 공유하도록 묶습니다.
/// 같은 컬렉션의 테스트는 순서대로 실행되므로 서로 간섭하지 않습니다.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
