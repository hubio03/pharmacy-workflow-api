namespace PharmacyWorkflow.Domain.Review;

/// <summary>중대 경고가 있는 처방을 확인 사유 없이 승인하려 할 때 발생합니다.</summary>
public class ReviewOverrideRequiredException : Exception
{
    public ReviewResult Review { get; }

    public ReviewOverrideRequiredException(ReviewResult review)
        : base($"중대 경고가 {review.CriticalCount}건 있어 검수 승인 시 확인 사유(note)가 필요합니다.")
    {
        Review = review;
    }
}
