namespace PharmacyWorkflow.Domain;

/// <summary>허용되지 않은 상태 전이를 시도할 때 발생하는 예외.</summary>
public class InvalidStatusTransitionException : Exception
{
    public PrescriptionStatus From { get; }
    public PrescriptionStatus To { get; }

    public InvalidStatusTransitionException(PrescriptionStatus from, PrescriptionStatus to)
        : base($"'{from}' 상태에서 '{to}' 상태로 변경할 수 없습니다.")
    {
        From = from;
        To = to;
    }
}
