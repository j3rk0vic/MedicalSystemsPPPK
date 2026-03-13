using MiniOrm.Attributes;

namespace MedicalSystem.Web.Models.Entities;

[Table("medications")]
public class Medication
{
    [PrimaryKey]
    [Column("id", IsNullable = false)]
    public int Id { get; set; }

    [Column("name", IsNullable = false, Length = 100)]
    public string Name { get; set; } = string.Empty;

    [Column("manufacturer", IsNullable = false, Length = 100)]
    public string Manufacturer { get; set; } = string.Empty;
    
    [Column("price", IsNullable = false)]
    public decimal Price { get; set; }
}