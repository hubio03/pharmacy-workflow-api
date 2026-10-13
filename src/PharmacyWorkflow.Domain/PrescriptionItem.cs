namespace PharmacyWorkflow.Domain;

/// <summary>처방 항목 (약품, 1회 용량, 1일 횟수, 투약 일수).</summary>
public sealed class PrescriptionItem
{
    public string DrugName { get; private set; }
    public decimal DosePerTime { get; private set; }
    public int TimesPerDay { get; private set; }
    public int Days { get; private set; }

    // 생성자 매개변수 이름이 속성 이름과 같으므로 EF Core가 이 생성자로 객체를 복원합니다.
    public PrescriptionItem(string drugName, decimal dosePerTime, int timesPerDay, int days)
    {
        if (string.IsNullOrWhiteSpace(drugName))
            throw new ArgumentException("약품명은 필수입니다.", nameof(drugName));
        if (dosePerTime <= 0)
            throw new ArgumentOutOfRangeException(nameof(dosePerTime), "1회 용량은 0보다 커야 합니다.");
        if (timesPerDay <= 0)
            throw new ArgumentOutOfRangeException(nameof(timesPerDay), "1일 횟수는 1 이상이어야 합니다.");
        if (days <= 0)
            throw new ArgumentOutOfRangeException(nameof(days), "투약 일수는 1 이상이어야 합니다.");

        DrugName = drugName;
        DosePerTime = dosePerTime;
        TimesPerDay = timesPerDay;
        Days = days;
    }
}
