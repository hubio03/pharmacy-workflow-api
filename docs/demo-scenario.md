# 시연 시나리오

약제부 업무 흐름을 처음부터 끝까지 보여 주는 시연 순서입니다. 대상은 처방 접수 → 자동 검수 → 승인 → 조제 → 불출 → 이력 확인입니다.
(모든 약품 정보와 계정은 학습용 가상 데이터입니다.)

## 준비

```bash
docker compose up --build -d api
```

브라우저에서 http://localhost:8080 (Swagger UI)을 엽니다.
오른쪽 위 **Authorize** 버튼에는 `POST /api/auth/login` 으로 받은 `accessToken` 값만 붙여 넣습니다.

> 자동으로 한 번에 보고 싶다면 Windows PowerShell에서 `powershell -ExecutionPolicy Bypass -File scripts\demo.ps1` 를 실행하세요.
> 아래 시나리오를 같은 순서로 호출하고 결과를 출력합니다.

## 시나리오

| # | 누가 | 무엇을 | 기대 결과 |
|---|---|---|---|
| 1 | 아무도 아님 | 토큰 없이 `GET /api/dispensing/pending` | **401** `UNAUTHORIZED` |
| 2 | `viewer01` | `POST /api/prescriptions` 접수 시도 | **403** `FORBIDDEN` (조회 전용은 접수 불가) |
| 3 | `doctor01` | 정상 처방 접수 (`샘플정 500mg`) | **201**, 상태 `Pending` |
| 4 | `doctor01` | 병용 금기 처방 접수 (`가나정 500mg` + `마바캡슐 100mg`) | **201** |
| 5 | `viewer01` | `GET /api/dispensing/{id}/review-result` (4번 처방) | `hasCritical: true`, `DRUG_INTERACTION` |
| 6 | `pharm01` | 4번 처방을 사유 없이 `verify` | **422** `REVIEW_OVERRIDE_REQUIRED` |
| 7 | `assist01` | 4번 처방 `verify` 시도 | **403** (조제 보조는 검수 불가) |
| 8 | `pharm01` | 4번 처방을 `{"note": "처방의 전화 확인 완료"}` 와 함께 `verify` | **200**, 상태 `Verified` |
| 9 | `assist01` | 4번 처방 `start` → `complete` | **200**, `InProgress` → `Completed` |
| 10 | `assist01` | 4번 처방 `release` 시도 | **403** (불출은 약사만) |
| 11 | `pharm01` | 4번 처방 `release` | **200**, 상태 `Released` |
| 12 | `pharm01` | 3번 처방에 바로 `start` (검수 전) | **409** `INVALID_STATUS_TRANSITION` |
| 13 | `viewer01` | `GET /api/prescriptions/{4번 id}/history` | 이력 4건: 처리자 `pharm01`, `assist01`, `assist01`, `pharm01`, 첫 이력에 확인 사유 |

## 확인 포인트

- **권한**: 같은 API라도 역할에 따라 403이 나뉩니다. (2, 7, 10)
- **자동 검수**: 병용 금기를 규칙 엔진이 찾아내고, 약사가 사유를 남겨야만 승인됩니다. (5, 6, 8)
- **상태 머신**: 순서를 어기는 호출은 서버가 막습니다. (12)
- **감사 로그**: 처리자는 토큰에서 기록되고 승인 사유가 남습니다. (13)
- **오류 형식**: 모든 오류가 `code`와 `traceId`가 포함된 같은 형식입니다.

## 데이터 유지 확인

처방을 접수한 뒤 컨테이너를 재시작해도 데이터가 남아 있어야 합니다.

```bash
docker compose restart api
```

같은 토큰(만료 전)으로 `GET /api/prescriptions/{id}` 를 다시 호출해 결과가 그대로인지 확인합니다.
`docker compose down -v` 를 실행하면 DB 볼륨까지 지워져 처음 상태로 돌아갑니다.
