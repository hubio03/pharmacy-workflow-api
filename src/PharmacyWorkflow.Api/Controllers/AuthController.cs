using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyWorkflow.Api.Auth;
using PharmacyWorkflow.Api.Contracts;

namespace PharmacyWorkflow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserStore _users;
    private readonly TokenService _tokens;

    public AuthController(IUserStore users, TokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    /// <summary>로그인하고 액세스 토큰(JWT)을 발급받습니다.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public ActionResult<LoginResponse> Login(LoginRequest request)
    {
        var user = _users.ValidateCredentials(request.UserId, request.Password)
                   ?? throw new AuthenticationFailedException();

        var (token, expiresAt) = _tokens.CreateToken(user);
        return new LoginResponse(token, expiresAt, user.Id, user.Role);
    }
}
