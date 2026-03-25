namespace MiniOrm.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class HasOneAttribute : Attribute
{
    public Type RelatedType { get; }
    public string ForeignKeyColumn { get; } // FK column on the RELATED entity    
   
    public HasOneAttribute(Type relatedType, string foreignKeyColumn)             
    {           
        RelatedType = relatedType;
        ForeignKeyColumn = foreignKeyColumn;
    }                                                                             
}