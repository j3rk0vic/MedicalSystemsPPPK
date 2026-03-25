namespace MiniOrm.Attributes;
                                                                                    
[AttributeUsage(AttributeTargets.Property)]
public class BelongsToAttribute : Attribute
{
    public Type RelatedType { get; }
    public string ForeignKeyColumn { get; } // FK column on THIS entity           
   
    public BelongsToAttribute(Type relatedType, string foreignKeyColumn)          
    {           
        RelatedType = relatedType;                                                
        ForeignKeyColumn = foreignKeyColumn;
    }
}