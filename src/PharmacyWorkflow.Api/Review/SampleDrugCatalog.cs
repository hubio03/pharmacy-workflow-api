using PharmacyWorkflow.Domain.Review;

namespace PharmacyWorkflow.Api.Review;

/// <summary>
/// 학습용 "가상" 약품·상호작용 데이터입니다. 실제 약품명, 함량, 금기 정보가 아니며
/// 실제 임상 판단에 사용할 수 없습니다. 나중에 DB나 공공 데이터 기반 구현체로 교체할 수 있습니다.
/// </summary>
public sealed class SampleDrugCatalog : IDrugCatalog
{
    private static readonly DrugInfo[] Drugs =
    {
        new("샘플정 500mg",     "샘플린", 500m, 3000m),
        new("가나정 500mg",     "가나돌", 500m, 3000m),
        new("가나플러스정 250mg", "가나돌", 250m, 3000m),
        new("다라정 250mg",     "다라민", 250m, 1500m),
        new("마바캡슐 100mg",   "마바신", 100m, 400m),
    };

    private readonly Dictionary<string, DrugInfo> _byName =
        Drugs.ToDictionary(d => d.Name, StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<(string, string), InteractionInfo> _interactions = new()
    {
        [Key("가나돌", "마바신")] = new(
            ReviewSeverity.Critical,
            "[학습용 가상 규칙] 가나돌과 마바신은 병용 금기입니다."),
        [Key("다라민", "마바신")] = new(
            ReviewSeverity.Caution,
            "[학습용 가상 규칙] 다라민과 마바신 병용 시 주의가 필요합니다."),
    };

    public DrugInfo? FindByName(string drugName)
    {
        if (string.IsNullOrWhiteSpace(drugName))
            return null;

        _byName.TryGetValue(drugName.Trim(), out var drug);
        return drug;
    }

    public InteractionInfo? FindInteraction(string ingredientA, string ingredientB)
    {
        _interactions.TryGetValue(Key(ingredientA, ingredientB), out var interaction);
        return interaction;
    }

    // 성분 순서와 관계없이 같은 키가 되도록 정렬해서 사용합니다.
    private static (string, string) Key(string a, string b)
    {
        a = a.Trim().ToLowerInvariant();
        b = b.Trim().ToLowerInvariant();
        return string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);
    }
}
