using MedicalSystem.Web.Data;
using MedicalSystem.Web.Models.Entities;                         
using Microsoft.AspNetCore.Mvc;

namespace MedicalSystem.Web.Controllers;

public class ExaminationsController : Controller
{
    private readonly MedicalDbContext _db;                       
 
    public ExaminationsController(MedicalDbContext db)           
    {           
        _db = db;
    }

    // GET /Examinations
    // Uses Include to show patient name in the list (many:1 navigation)                                                      
    public IActionResult Index(int? patientId)
    {                                                          
        List<Examination> examinations;
                                                             
        if (patientId.HasValue)
        {
            // Fluent API: Where() + Include() chained together
            examinations = _db.Examinations                    
                .Where(e => e.PatientId == patientId.Value)
                .ToList();                                     
                  
            // Load patient names separately for filtered results         
                examinations = _db.Examinations                    
                    .Include(e => e.Patient)
                    .Where(e => e.PatientId == patientId.Value)    
                    .ToList();
        }                                                      
        else        
        {
            examinations = _db.Examinations
                .Include(e => e.Patient)
                .ToList();                                     
        }
                                                             
        ViewBag.Patients          = _db.Patients.GetAll();
        ViewBag.SelectedPatientId = patientId;
        return View(examinations);                             
    }
                                                                 
    // GET /Examinations/Details/5
    // Demonstrates 1:many (Prescriptions) and many:1 (Patient)
    public IActionResult Details(int id)                         
    {
        var examination = _db.Examinations                       
            .Include(e => e.Patient)
            .Include(e => e.Prescriptions)
            .Where(e => e.Id == id)                              
            .ToList()
            .FirstOrDefault();                                   
                
        if (examination == null) return NotFound();

        // Load medication names for each prescription           
        ViewBag.Medications = _db.Medications.GetAll();
        return View(examination);                                
    }           
                                                                 
    // GET /Examinations/Create
    public IActionResult Create()                                
    {           
        ViewBag.Patients = _db.Patients.GetAll();
        return View();
    }

    // POST /Examinations/Create                                 
    [HttpPost]
    public IActionResult Create(Examination examination)         
    {           
        _db.Examinations.Insert(examination);
        return RedirectToAction(nameof(Index));
    }

    // GET /Examinations/Edit/5                                  
    public IActionResult Edit(int id)
    {                                                            
        var examination = _db.Examinations.GetById(id);
        if (examination == null) return NotFound();
        ViewBag.Patients = _db.Patients.GetAll();
        return View(examination);                                
    }
                                                                 
    // POST /Examinations/Edit/5
    [HttpPost]
    public IActionResult Edit(Examination examination)
    {
        _db.Examinations.Update(examination);
        return RedirectToAction(nameof(Index));
    }                                                            
 
    // GET /Examinations/Delete/5                                
    public IActionResult Delete(int id)
    {
        var examination = _db.Examinations
            .Include(e => e.Patient)
            .Where(e => e.Id == id)
            .ToList()                                            
            .FirstOrDefault();
        if (examination == null) return NotFound();              
        return View(examination);
    }

    // POST /Examinations/Delete/5                               
    [HttpPost, ActionName("Delete")]
    public IActionResult DeleteConfirmed(int id)                 
    {           
        _db.Examinations.Delete(id);
        return RedirectToAction(nameof(Index));
    }
}