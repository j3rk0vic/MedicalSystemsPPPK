using MedicalSystem.Web.Data;
using MedicalSystem.Web.Models.Entities;                         
using Microsoft.AspNetCore.Mvc;
                                                                   
namespace MedicalSystem.Web.Controllers;
                                                                 
public class PatientsController : Controller
{
    private readonly MedicalDbContext _db;

    public PatientsController(MedicalDbContext db)
    {
        _db = db;                                                
    }
                                                                 
    // GET /Patients
    public IActionResult Index()
    {
        var patients = _db.Patients.GetAll();
        return View(patients);                                   
    }
                                                                 
    // GET /Patients/Details/5
    // Demonstrates navigation properties — loads MedicalRecord (1:1) and Examinations (1:many)                                  
    public IActionResult Details(int id)
    {                                                            
        var patient = _db.Patients
            .Include(p => p.MedicalRecord)                       
            .Include(p => p.Examinations)
            .Where(p => p.Id == id)                              
            .ToList()
            .FirstOrDefault();

        if (patient == null) return NotFound();                  
        return View(patient);
    }                                                            
                
    // GET /Patients/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST /Patients/Create
    [HttpPost]
    public IActionResult Create(Patient patient)                 
    {
        _db.Patients.Insert(patient);                            
        return RedirectToAction(nameof(Index));
    }
                                                                 
    // GET /Patients/Edit/5
    public IActionResult Edit(int id)                            
    {           
        var patient = _db.Patients.GetById(id);
        if (patient == null) return NotFound();
        return View(patient);                                    
    }
                                                                 
    // POST /Patients/Edit/5
    [HttpPost]
    public IActionResult Edit(Patient patient)
    {
        _db.Patients.Update(patient);
        return RedirectToAction(nameof(Index));
    }                                                            
 
    // GET /Patients/Delete/5                                    
    public IActionResult Delete(int id)
    {
        var patient = _db.Patients.GetById(id);
        if (patient == null) return NotFound();
        return View(patient);                                    
    }
                                                                 
    // POST /Patients/Delete/5
    [HttpPost, ActionName("Delete")]
    public IActionResult DeleteConfirmed(int id)
    {                                                            
        _db.Patients.Delete(id);
        return RedirectToAction(nameof(Index));                  
    }           
}