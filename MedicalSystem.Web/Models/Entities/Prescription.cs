using MiniOrm.Attributes;

namespace MedicalSystem.Web.Models.Entities;

[Table("prescriptions")]
public class Prescription
{
    [PrimaryKey]
    [Column("id", IsNullable = false)]
    public int Id { get; set; }
    
    
    [Column("examination_id", IsNullable = false)]
    [ForeignKey("examinations")]
    public int ExaminationId { get; set; }
    
    [Column("medication_id", IsNullable = false)]
    [ForeignKey("medications")]
    public int MedicationId { get; set; }
    
    [Column("dosage", IsNullable = false)]
    public string Dosage { get; set; } = string.Empty;

    [Column("frequency", IsNullable = false)]
    public string Frequency { get; set; } = string.Empty;
    
    [Column("duration_days", IsNullable = false)]
    public int DurationDays { get; set; }
    
    [BelongsTo(typeof(Examination), "examination_id")]                                
    public Examination? Examination { get; set; }     
                                                                                    
    [BelongsTo(typeof(Medication), "medication_id")]
    public Medication? Medication { get; set; }
}