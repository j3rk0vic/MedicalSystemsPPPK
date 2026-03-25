namespace MiniOrm.Attributes;
                                                                                    
[AttributeUsage(AttributeTargets.Property)]
public class HasManyAttribute : Attribute                                         
{               
    public Type RelatedType { get; }
    public string ForeignKeyColumn { get; } // FK column on the RELATED entity

    public HasManyAttribute(Type relatedType, string foreignKeyColumn)            
    {
        RelatedType = relatedType;                                                
        ForeignKeyColumn = foreignKeyColumn;
    }
}