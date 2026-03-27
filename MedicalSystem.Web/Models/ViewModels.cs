namespace MedicalSystem.Web.Models.ViewModels;             
                  
public class CreatePatientViewModel
{
    // Patient fields
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;   
    public string Oib { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }              
                                                             
    // MedicalRecord fields
    public string BloodType { get; set; } = string.Empty;  
    public string Allergies { get; set; } = string.Empty;
    public string ChronicDiseases { get; set; } = string.Empty;                                              
}