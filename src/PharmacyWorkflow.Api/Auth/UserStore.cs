using Microsoft.AspNetCore.Identity;

namespace PharmacyWorkflow.Api.Auth;

public sealed record AppUser(string Id, string DisplayName, string Role, string PasswordHash);

public interface IUserStore
{
    /// <summary>자격 증명이 맞으면 사용자를, 아니면 null을 반환합니다.</summary>
    AppUser? ValidateCredentials(string userId, string password);
}

/// <summary>
/// 학습용 인메모리 사용자 저장소(데모 계정). 비밀번호는 평문이 아니라 해시로만 보관합니다.
/// 실제 서비스에서는 DB 기반 저장소와 계정 관리 절차로 교체해야 합니다.
/// </summary>
public sealed class InMemoryUserStore : IUserStore
{
    /// <summary>데모 계정 공통 비밀번호 (학습용)</summary>
    public const string DemoPassword = "Demo1234!";

    private readonly PasswordHasher<AppUser> _hasher = new();
    private readonly Dictionary<string, AppUser> _users = new(StringComparer.OrdinalIgnoreCase);
    private readonly AppUser _dummyUser = new("dummy", "dummy", Roles.Viewer, string.Empty);
    private readonly string _dummyHash;

    public InMemoryUserStore()
    {
        Add("doctor01", "처방의 데모", Roles.Prescriber);
        Add("pharm01", "약사 데모", Roles.Pharmacist);
        Add("assist01", "조제 보조 데모", Roles.Assistant);
        Add("viewer01", "조회 전용 데모", Roles.Viewer);

        _dummyHash = _hasher.HashPassword(_dummyUser, DemoPassword);
    }

    public AppUser? ValidateCredentials(string userId, string password)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrEmpty(password))
            return null;

        if (!_users.TryGetValue(userId.Trim(), out var user))
        {
            // 없는 계정도 같은 계산 비용을 쓰게 해서 응답 시간으로 계정 존재 여부를 알기 어렵게 합니다.
            _hasher.VerifyHashedPassword(_dummyUser, _dummyHash, password);
            return null;
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    private void Add(string id, string displayName, string role)
    {
        var user = new AppUser(id, displayName, role, string.Empty);
        _users[id] = user with { PasswordHash = _hasher.HashPassword(user, DemoPassword) };
    }
}
