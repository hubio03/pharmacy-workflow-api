namespace PharmacyWorkflow.Domain.Review;

/// <summary>같은 성분이 서로 다른(또는 같은) 약품으로 중복 처방되었는지 검사합니다.</summary>
public sealed class DuplicateIngredientRule : IReviewRule
{
    private readonly IDrugCatalog _catalog;

    public DuplicateIngredientRule(IDrugCatalog catalog) => _catalog = catalog;

    public string Code => "DUPLICATE_INGREDIENT";

    public IEnumerable<ReviewFinding> Evaluate(Prescription prescription)
    {
        var byIngredient = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in prescription.Items)
        {
            var drug = _catalog.FindByName(item.DrugName);
            if (drug is null)
                continue;

            if (!byIngredient.TryGetValue(drug.Ingredient, out var names))
            {
                names = new List<string>();
                byIngredient[drug.Ingredient] = names;
            }

            names.Add(item.DrugName);
        }

        foreach (var (ingredient, names) in byIngredient)
        {
            if (names.Count > 1)
            {
                yield return new ReviewFinding(
                    Code,
                    ReviewSeverity.Caution,
                    $"동일 성분 '{ingredient}'이(가) 중복 처방되었습니다: {string.Join(", ", names)}",
                    names);
            }
        }
    }
}
