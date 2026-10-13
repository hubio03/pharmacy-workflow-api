using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using PharmacyWorkflow.Api.Auth;

namespace PharmacyWorkflow.Domain.Tests;

public class AuthUnitTests
{
    [Fact]
    public void 올바른_자격_증명이면_사용자를_반환한다()
    {
        var store = new InMemoryUserStore();

        var user = store.ValidateCredentials("pharm01", InMemoryUserStore.DemoPassword);

        Assert.NotNull(user);
        Assert.Equal(Roles.Pharmacist, user!.Role);
    }

    [Fact]
    public void 사용자_ID는_대소문자를_구분하지_않는다()
    {
        var store = new InMemoryUserStore();

        Assert.NotNull(store.ValidateCredentials("PHARM01", InMemoryUserStore.DemoPassword));
    }

    [Theory]
    [InlineData("pharm01", "wrong-password")]
    [InlineData("nobody", "Demo1234!")]
    [InlineData("pharm01", "")]
    [InlineData("", "Demo1234!")]
    public void 잘못된_자격_증명이면_null을_반환한다(string userId, string password)
    {
        var store = new InMemoryUserStore();

        Assert.Null(store.ValidateCredentials(userId, password));
    }

    [Fact]
    public void 비밀번호는_평문으로_저장되지_않는다()
    {
        var store = new InMemoryUserStore();
        var user = store.ValidateCredentials("pharm01", InMemoryUserStore.DemoPassword)!;

        Assert.DoesNotContain(InMemoryUserStore.DemoPassword, user.PasswordHash);
    }

    [Fact]
    public void 발급한_토큰에_사용자_ID와_역할이_담긴다()
    {
        var options = Options.Create(new JwtOptions { Key = "unit-test-key-0123456789-0123456789" });
        var tokens = new TokenService(options);
        var user = new AppUser("pharm01", "약사 데모", Roles.Pharmacist, string.Empty);

        var (token, expiresAt) = tokens.CreateToken(user);
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);

        Assert.Equal("pharm01", jwt.Subject);
        Assert.Equal(Roles.Pharmacist, jwt.Claims.First(c => c.Type == "role").Value);
        Assert.True(expiresAt > DateTimeOffset.UtcNow);
    }
}
