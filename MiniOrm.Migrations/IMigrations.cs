namespace MiniOrm.Migrations;
                                                                   
public interface IMigration
{
    string Name { get; }                                                
    string Up();          // SQL to apply
    string Down();        // SQL to revert                       
}