namespace MiniOrm.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ForeignKeyAttribute : Attribute
{
    public string ReferenceTable { get; }
    public string ReferenceColumn { get; }

    public ForeignKeyAttribute(string referenceTable, string referenceColumn = "id")
    {
        ReferenceTable = referenceTable;
        ReferenceColumn = referenceColumn;
    }
}

