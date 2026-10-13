namespace PharmacyWorkflow.Domain.Review;

/// <summary>약품 정보. 1회 투여 단위(정/캡슐) 한 개의 함량(mg)과 성분을 가집니다.</summary>
public sealed record DrugInfo(string Name, string Ingredient, decimal StrengthMg, decimal? MaxDailyDoseMg);

/// <summary>두 성분 사이의 상호작용 정보.</summary>
public sealed record InteractionInfo(ReviewSeverity Severity, string Description);

/// <summary>
/// 검수 규칙이 참조하는 약품 정보 출처. 지금은 샘플 데이터 구현체를 쓰고,
/// 나중에 DB나 공공 데이터 기반 구현체로 바꿔도 규칙 코드는 그대로입니다.
/// </summary>
public interface IDrugCatalog
{
    DrugInfo? FindByName(string drugName);

    /// <summary>두 성분 사이의 상호작용을 조회합니다. (성분 순서는 무관)</summary>
    InteractionInfo? FindInteraction(string ingredientA, string ingredientB);
}
