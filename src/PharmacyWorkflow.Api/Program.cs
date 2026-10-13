using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PharmacyWorkflow.Api.Auth;
using PharmacyWorkflow.Api.Data;
using PharmacyWorkflow.Api.Errors;
using PharmacyWorkflow.Api.Repositories;
using PharmacyWorkflow.Api.Review;
using PharmacyWorkflow.Api.Services;
using PharmacyWorkflow.Domain.Review;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ---------- 표준 오류 응답 (ProblemDetails + code + traceId) ----------
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
    {
        var problem = context.ProblemDetails;
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        if (problem.Extensions.ContainsKey("code"))
            return; // 예외 처리기나 입력 검증에서 이미 코드를 지정한 경우

        switch (problem.Status)
        {
            case StatusCodes.Status401Unauthorized:
                problem.Title = "인증이 필요합니다";
                problem.Detail = "POST /api/auth/login 으로 로그인한 뒤, 발급된 토큰을 Authorization: Bearer 헤더로 보내 주세요.";
                problem.Extensions["code"] = "UNAUTHORIZED";
                break;
            case StatusCodes.Status403Forbidden:
                problem.Title = "접근 권한이 없습니다";
                problem.Detail = "이 작업을 수행할 수 있는 역할이 아닙니다.";
                problem.Extensions["code"] = "FORBIDDEN";
                break;
            case StatusCodes.Status404NotFound:
                problem.Title = "리소스를 찾을 수 없음";
                problem.Extensions["code"] = "NOT_FOUND";
                break;
            case StatusCodes.Status500InternalServerError:
                problem.Title = "서버 오류";
                problem.Extensions["code"] = "INTERNAL_ERROR";
                break;
            default:
                problem.Extensions["code"] = "ERROR";
                break;
        }
    });

// 입력 검증 실패(400)도 같은 형식으로
builder.Services.Configure<ApiBehaviorOptions>(options =>
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "입력값 검증 실패",
        };
        problem.Extensions["code"] = "VALIDATION_ERROR";
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        var result = new BadRequestObjectResult(problem);
        result.ContentTypes.Add("application/problem+json");
        return result;
    });

builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// ---------- 인증(JWT) ----------
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key) || Encoding.UTF8.GetByteCount(jwt.Key) < 32)
    throw new InvalidOperationException("Jwt:Key 는 32바이트 이상이어야 합니다. 환경 변수 Jwt__Key 로 설정해 주세요.");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // 토큰의 sub/name/role 이름을 그대로 사용
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role",
        };
    });

// ---------- 권한(역할 기반) ----------
// 기본값: 어떤 엔드포인트든 로그인(유효한 토큰)이 필요합니다. 로그인 API만 예외로 열어 둡니다.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy(Policies.SubmitPrescription, p => p.RequireRole(Roles.Prescriber, Roles.Pharmacist))
    .AddPolicy(Policies.VerifyPrescription, p => p.RequireRole(Roles.Pharmacist))
    .AddPolicy(Policies.Dispense, p => p.RequireRole(Roles.Pharmacist, Roles.Assistant))
    .AddPolicy(Policies.ReleasePrescription, p => p.RequireRole(Roles.Pharmacist));

// ---------- Swagger (Authorize 버튼으로 토큰 입력) ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "약제부 처방 검수·조제 워크플로우 API",
        Version = "v1",
        Description = "병원 약제부의 처방 접수 → 검수 → 조제 → 불출 흐름을 관리하는 학습용 API입니다. "
                      + "실제 환자 정보를 사용하지 않으며, 실제 임상 판단을 대체할 수 없습니다.",
    });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, "PharmacyWorkflow.Api.xml");
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "POST /api/auth/login 으로 받은 accessToken 값만 입력하세요.",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ---------- 저장소 / 서비스 ----------
var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? "Data Source=pharmacy.db";

builder.Services.AddDbContext<PharmacyDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IPrescriptionRepository, EfPrescriptionRepository>();

builder.Services.AddSingleton<IDrugCatalog, SampleDrugCatalog>();
builder.Services.AddSingleton<PrescriptionReviewer>(sp =>
    new PrescriptionReviewer(DefaultReviewRules.Create(sp.GetRequiredService<IDrugCatalog>())));

builder.Services.AddScoped<DispensingService>();

var app = builder.Build();

// 시작 시 DB 준비: 마이그레이션 파일이 있으면 적용하고, 아직 없으면 모델 기준으로 테이블을 생성합니다.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();

    if (db.Database.GetMigrations().Any())
        db.Database.Migrate();
    else
        db.Database.EnsureCreated();
}

app.UseExceptionHandler();
app.UseStatusCodePages();   // 본문 없는 401/403/404 등도 표준 오류 형식으로 채워 줍니다.

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();
app.MapControllers();

app.Run();

// 통합 테스트(WebApplicationFactory)에서 사용하기 위한 선언
public partial class Program { }
