using MiniOrm.Attributes;

namespace MedicalSystem.Web.Models.Entities;

[Table("medical_records")]
public class MedicalRecord
{
    [PrimaryKey]
    [Column("id", IsNullable = false)]
    public int Id { get; set; }
    
    [Column("patient_id", IsNullable = false)]
    [ForeignKey("patients")]
    public int PatientId { get; set; }

    [Column("blood_type", IsNullable = false, Length = 5)]
    public string BloodType { get; set; } = string.Empty;

    [Column("allergies")]
    public string Allergies { get; set; } = string.Empty;

    [Column("chronic_diseases", IsNullable = false)]
    public string ChronicDiseases { get; set; } = string.Empty;
}