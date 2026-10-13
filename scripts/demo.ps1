<#
  약제부 처방 검수·조제 워크플로우 API 시연 스크립트 (학습용)

  사용법:
    docker compose up --build -d api
    powershell -ExecutionPolicy Bypass -File scripts\demo.ps1

  docs\demo-scenario.md 의 시나리오를 같은 순서로 호출하고 결과를 출력합니다.
#>
param([string]$BaseUrl = "http://localhost:8080")

$Password = "Demo1234!"

function Invoke-Api {
    param([string]$Method, [string]$Path, [string]$Token, $Body)

    $headers = @{}
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }

    $params = @{
        Method          = $Method
        Uri             = "$BaseUrl$Path"
        Headers         = $headers
        UseBasicParsing = $true
    }
    if ($null -ne $Body) {
        $json = $Body | ConvertTo-Json -Depth 6
        $params["Body"] = [System.Text.Encoding]::UTF8.GetBytes($json)
        $params["ContentType"] = "application/json; charset=utf-8"
    }

    try {
        $response = Invoke-WebRequest @params
        $data = $null
        if ($response.Content) { $data = $response.Content | ConvertFrom-Json }
        return [pscustomobject]@{ Status = [int]$response.StatusCode; Data = $data }
    }
    catch {
        $status = 0
        if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }

        $text = $null
        if ($_.ErrorDetails -and $_.ErrorDetails.Message) {
            $text = $_.ErrorDetails.Message
        }
        elseif ($_.Exception.Response -and $_.Exception.Response.GetResponseStream) {
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream(), [System.Text.Encoding]::UTF8)
                $text = $reader.ReadToEnd()
            } catch { }
        }

        $data = $null
        if ($text) { try { $data = $text | ConvertFrom-Json } catch { } }
        return [pscustomobject]@{ Status = $status; Data = $data }
    }
}

function Write-Step {
    param([string]$Title, $Result, [string]$Expect)

    $mark = if ($Result.Status -ge 200 -and $Result.Status -lt 400) { "OK " } else { "ERR" }
    $code = ""
    if ($Result.Data -and $Result.Data.code) { $code = " code=" + $Result.Data.code }
    Write-Host ("[{0}] {1} -> HTTP {2}{3}   (기대: {4})" -f $mark, $Title, $Result.Status, $code, $Expect)
}

# ---------- 준비: 로그인 ----------
Write-Host ""
Write-Host "== 1. 로그인 (역할별 토큰 발급) =="
$token = @{}
foreach ($user in "doctor01", "pharm01", "assist01", "viewer01") {
    $r = Invoke-Api "POST" "/api/auth/login" $null @{ userId = $user; password = $Password }
    if ($r.Status -ne 200) {
        Write-Host "로그인에 실패했습니다. API가 $BaseUrl 에서 실행 중인지 확인하세요."
        return
    }
    $token[$user] = $r.Data.accessToken
    Write-Step "$user 로그인 (역할: $($r.Data.role))" $r "200"
}

$normal = @{
    patientId = "PT-DEMO-1"; prescriberId = "DOC-01"
    items = @( @{ drugName = "샘플정 500mg"; dosePerTime = 1; timesPerDay = 3; days = 5 } )
}
$risky = @{
    patientId = "PT-DEMO-2"; prescriberId = "DOC-01"
    items = @(
        @{ drugName = "가나정 500mg";   dosePerTime = 1; timesPerDay = 2; days = 5 },
        @{ drugName = "마바캡슐 100mg"; dosePerTime = 1; timesPerDay = 2; days = 5 }
    )
}

# ---------- 인증 / 권한 ----------
Write-Host ""
Write-Host "== 2. 인증과 권한 =="
$r = Invoke-Api "GET" "/api/dispensing/pending" $null $null
Write-Step "토큰 없이 대기 목록 조회" $r "401"

$r = Invoke-Api "POST" "/api/prescriptions" $token["viewer01"] $normal
Write-Step "조회 전용 계정이 처방 접수" $r "403"

# ---------- 접수와 자동 검수 ----------
Write-Host ""
Write-Host "== 3. 처방 접수와 자동 검수 =="
$r = Invoke-Api "POST" "/api/prescriptions" $token["doctor01"] $normal
Write-Step "정상 처방 접수" $r "201"
$normalId = $r.Data.id

$r = Invoke-Api "POST" "/api/prescriptions" $token["doctor01"] $risky
Write-Step "병용 금기 처방 접수" $r "201"
$riskyId = $r.Data.id

$r = Invoke-Api "GET" "/api/dispensing/$riskyId/review-result" $token["viewer01"] $null
Write-Step "검수 결과 조회" $r "200"
foreach ($f in $r.Data.findings) {
    Write-Host ("      - [{0}] {1}: {2}" -f $f.severity, $f.ruleCode, $f.message)
}

# ---------- 검수 승인 ----------
Write-Host ""
Write-Host "== 4. 검수 승인 (중대 경고는 사유 필수) =="
$r = Invoke-Api "POST" "/api/dispensing/$riskyId/verify" $token["pharm01"] @{}
Write-Step "사유 없이 승인" $r "422"

$r = Invoke-Api "POST" "/api/dispensing/$riskyId/verify" $token["assist01"] @{ note = "시도" }
Write-Step "조제 보조가 승인 시도" $r "403"

$r = Invoke-Api "POST" "/api/dispensing/$riskyId/verify" $token["pharm01"] @{ note = "처방의 전화 확인 완료" }
Write-Step "사유와 함께 승인" $r "200"

# ---------- 조제와 불출 ----------
Write-Host ""
Write-Host "== 5. 조제와 불출 =="
$r = Invoke-Api "POST" "/api/dispensing/$riskyId/start" $token["assist01"] $null
Write-Step "조제 시작 (조제 보조)" $r "200"

$r = Invoke-Api "POST" "/api/dispensing/$riskyId/complete" $token["assist01"] $null
Write-Step "조제 완료 (조제 보조)" $r "200"

$r = Invoke-Api "POST" "/api/dispensing/$riskyId/release" $token["assist01"] $null
Write-Step "조제 보조가 불출 시도" $r "403"

$r = Invoke-Api "POST" "/api/dispensing/$riskyId/release" $token["pharm01"] $null
Write-Step "불출 (약사)" $r "200"

# ---------- 상태 머신 ----------
Write-Host ""
Write-Host "== 6. 상태 전이 규칙 =="
$r = Invoke-Api "POST" "/api/dispensing/$normalId/start" $token["pharm01"] $null
Write-Step "검수 전에 조제 시작" $r "409"

# ---------- 감사 로그 ----------
Write-Host ""
Write-Host "== 7. 감사 로그 (병용 금기 처방) =="
$r = Invoke-Api "GET" "/api/prescriptions/$riskyId/history" $token["viewer01"] $null
Write-Step "상태 변경 이력 조회" $r "200"
foreach ($h in $r.Data) {
    Write-Host ("      {0} -> {1}  처리자: {2}  사유: {3}" -f $h.from, $h.to, $h.actorId, $h.reason)
}

Write-Host ""
Write-Host "시연이 끝났습니다. (모든 데이터는 학습용 가상 데이터입니다.)"
