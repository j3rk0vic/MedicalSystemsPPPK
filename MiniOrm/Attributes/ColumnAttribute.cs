namespace MiniOrm.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ColumnAttribute : Attribute
{
    public string Name { get; }
    public bool IsNullable { get; set; } = true;
    public int Length { get; set; } = 255;
    public string? DefaultValue { get; set; }

    public ColumnAttribute(string name)
    {
        Name = name;
    }
}