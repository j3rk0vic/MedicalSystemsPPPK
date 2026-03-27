using MedicalSystem.Web.Data;
using MedicalSystem.Web.Models.Entities;                         
using Microsoft.AspNetCore.Mvc;

namespace MedicalSystem.Web.Controllers;                         
   
public class MedicationsController : Controller                  
{               
    private readonly MedicalDbContext _db;

    public MedicationsController(MedicalDbContext db)
    {
        _db = db;
    }

    // GET /Medications
    public IActionResult Index(string? sortBy)
    {                                                          
        var medications = sortBy switch
        {                                                      
            "price" => _db.Medications.OrderBy(m => m.Price).ToList(),                                         
            "price_desc" => _db.Medications.OrderByDescending(m => m.Price).ToList(),                                     
            "name" => _db.Medications.OrderBy(m => m.Name).ToList(),_ => _db.Medications.GetAll()
        };                                                     
   
        ViewBag.SortBy = sortBy;                               
        return View(medications);
    }         

    // GET /Medications/Create
    public IActionResult Create()
    {
        return View();
    }                                                            
 
    // POST /Medications/Create                                  
    [HttpPost]  
    public IActionResult Create(Medication medication)
    {
        _db.Medications.Insert(medication);
        return RedirectToAction(nameof(Index));
    }
                                                                 
    // GET /Medications/Edit/5
    public IActionResult Edit(int id)                            
    {           
        var medication = _db.Medications.GetById(id);
        if (medication == null) return NotFound();
        return View(medication);
    }

    // POST /Medications/Edit/5                                  
    [HttpPost]
    public IActionResult Edit(Medication medication)             
    {           
        _db.Medications.Update(medication);
        return RedirectToAction(nameof(Index));
    }

    // GET /Medications/Delete/5                                 
    public IActionResult Delete(int id)
    {                                                            
        var medication = _db.Medications.GetById(id);
        if (medication == null) return NotFound();
        return View(medication);
    }

    // POST /Medications/Delete/5                                
    [HttpPost, ActionName("Delete")]
    public IActionResult DeleteConfirmed(int id)                 
    {           
        _db.Medications.Delete(id);
        return RedirectToAction(nameof(Index));
    }
}