using MiniOrm.Attributes;

namespace MedicalSystem.Web.Models.Entities;

[Table("examinations")]
public class Examination
{
    [PrimaryKey]
    [Column("id", IsNullable = false)]
    public int Id { get; set; }
    
    [Column("patient_id", IsNullable = false)]
    [ForeignKey("patients")]
    public int PatientId { get; set; }
    
    [Column("examination_type", IsNullable = false)]
    public ExaminationType Type { get; set; }
    
    [Column("date", IsNullable = false)]
    public DateTime Date { get; set; }

    [Column("diagnosis")] 
    public string Diagnosis { get; set; } = string.Empty;

    [Column("notes")] 
    public string Notes { get; set; } = string.Empty;
    
    [BelongsTo(typeof(Patient), "patient_id")]                                        
    public Patient? Patient { get; set; }     
                                       
    [HasMany(typeof(Prescription), "examination_id")]                                 
    public List<Prescription> Prescriptions { get; set; } = new();

}