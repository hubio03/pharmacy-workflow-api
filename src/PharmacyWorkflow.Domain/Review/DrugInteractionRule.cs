namespace PharmacyWorkflow.Domain.Review;

/// <summary>처방에 포함된 서로 다른 성분 쌍마다 상호작용 정보를 조회해 경고합니다.</summary>
public sealed class DrugInteractionRule : IReviewRule
{
    private readonly IDrugCatalog _catalog;

    public DrugInteractionRule(IDrugCatalog catalog) => _catalog = catalog;

    public string Code => "DRUG_INTERACTION";

    public IEnumerable<ReviewFinding> Evaluate(Prescription prescription)
    {
        // 성분별로 묶어서, 같은 성분끼리는 비교하지 않고 서로 다른 성분 쌍만 비교합니다.
        var groups = new List<(string Ingredient, List<string> DrugNames)>();

        foreach (var item in prescription.Items)
        {
            var drug = _catalog.FindByName(item.DrugName);
            if (drug is null)
                continue;

            var index = groups.FindIndex(g =>
                string.Equals(g.Ingredient, drug.Ingredient, StringComparison.OrdinalIgnoreCase));

            if (index < 0)
                groups.Add((drug.Ingredient, new List<string> { item.DrugName }));
            else if (!groups[index].DrugNames.Contains(item.DrugName))
                groups[index].DrugNames.Add(item.DrugName);
        }

        for (var i = 0; i < groups.Count; i++)
        {
            for (var j = i + 1; j < groups.Count; j++)
            {
                var interaction = _catalog.FindInteraction(groups[i].Ingredient, groups[j].Ingredient);
                if (interaction is null)
                    continue;

                yield return new ReviewFinding(
                    Code,
                    interaction.Severity,
                    interaction.Description,
                    groups[i].DrugNames.Concat(groups[j].DrugNames).ToList());
            }
        }
    }
}
