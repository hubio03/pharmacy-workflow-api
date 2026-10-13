namespace PharmacyWorkflow.Api.Auth;

/// <summary>사용자 ID 또는 비밀번호가 올바르지 않을 때 발생합니다.</summary>
public class AuthenticationFailedException : Exception
{
    // 어느 쪽이 틀렸는지 알려주지 않도록 메시지는 항상 같게 유지합니다.
    public AuthenticationFailedException()
        : base("사용자 ID 또는 비밀번호가 올바르지 않습니다.")
    {
    }
}
