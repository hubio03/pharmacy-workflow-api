using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PharmacyWorkflow.Domain;

namespace PharmacyWorkflow.Api.Data;

/// <summary>
/// Prescription 집합체의 DB 매핑. 도메인 클래스는 EF Core를 전혀 모르고, 매핑은 이 파일에만 둡니다.
/// 처방 항목(Items)과 상태 변경 이력(History)은 처방에 종속된 "소유 엔티티"로 저장합니다.
/// </summary>
public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        // SQLite는 DateTimeOffset 정렬/비교를 지원하지 않으므로 long 값으로 변환해 저장합니다.
        var dateConverter = new DateTimeOffsetToBinaryConverter();

        builder.ToTable("Prescriptions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.PatientId).IsRequired().HasMaxLength(50);
        builder.Property(p => p.PrescriberId).IsRequired().HasMaxLength(50);
        builder.Property(p => p.PrescribedAt).HasConversion(dateConverter);

        // 상태는 문자열로 저장(DB를 직접 봐도 읽기 쉬움).
        // 동시성 토큰: 저장 시 "읽었을 때의 상태 그대로일 때만" UPDATE 되어, 동시에 같은 처방을 바꾸는 충돌을 감지합니다.
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .IsConcurrencyToken();

        builder.HasIndex(p => p.Status);

        builder.OwnsMany(p => p.Items, items =>
        {
            items.ToTable("PrescriptionItems");
            items.WithOwner().HasForeignKey("PrescriptionId");
            items.Property<int>("Id");
            items.HasKey("Id");

            items.Property(i => i.DrugName).IsRequired().HasMaxLength(200);
        });

        builder.OwnsMany(p => p.History, history =>
        {
            history.ToTable("ReviewLogs");
            history.WithOwner().HasForeignKey("PrescriptionId");
            history.Property<int>("Id");      // 자동 증가 키 → 이력이 저장된 순서대로 조회됨
            history.HasKey("Id");

            history.Property(h => h.From).HasConversion<string>().HasMaxLength(20);
            history.Property(h => h.To).HasConversion<string>().HasMaxLength(20);
            history.Property(h => h.ActorId).IsRequired().HasMaxLength(50);
            history.Property(h => h.Reason).HasMaxLength(500);
            history.Property(h => h.At).HasConversion(dateConverter);
        });

        // 읽기 전용 속성(Items, History) 대신 private 필드(_items, _history)에 직접 값을 채웁니다.
        builder.Navigation(p => p.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.History).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
