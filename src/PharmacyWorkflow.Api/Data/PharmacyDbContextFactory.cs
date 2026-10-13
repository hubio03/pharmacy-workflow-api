using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PharmacyWorkflow.Api.Data;

/// <summary>`dotnet ef migrations` 같은 도구가 앱을 실행하지 않고 DbContext를 만들 때 사용합니다.</summary>
public class PharmacyDbContextFactory : IDesignTimeDbContextFactory<PharmacyDbContext>
{
    public PharmacyDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PharmacyDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;

        return new PharmacyDbContext(options);
    }
}
