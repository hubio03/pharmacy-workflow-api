namespace PharmacyWorkflow.Domain.Review;

/// <summary>검수 규칙 하나. 새 규칙은 이 인터페이스를 구현하는 클래스를 추가하면 됩니다.</summary>
public interface IReviewRule
{
    string Code { get; }

    IEnumerable<ReviewFinding> Evaluate(Prescription prescription);
}
