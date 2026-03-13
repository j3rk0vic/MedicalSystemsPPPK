using MiniOrm.Attributes;

namespace MedicalSystem.Web.Models.Entities;

[Table("patients")]
public class Patient
{
    [PrimaryKey]
    [Column("id", IsNullable = false)]
    public int Id { get; set; }
    
    [Column("first_name", IsNullable = false, Length = 100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Column("last_name", IsNullable = false, Length = 100)]
    public string LastName { get; set; } = string.Empty;
    
    [Unique]
    [Column("oib", IsNullable = false, Length = 11)]
    public string Oib { get; set; } = string.Empty;
    
    [Column("date_of_birth", IsNullable = false)]
    public DateTime DateOfBirth { get; set; }
}