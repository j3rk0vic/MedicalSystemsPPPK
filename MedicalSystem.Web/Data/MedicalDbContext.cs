using MedicalSystem.Web.Models.Entities;
using MiniOrm.Database;

namespace MedicalSystem.Web.Data;

public class MedicalDbContext : DbContext
{
    public DbSet<Patient> Patients { get; }
    public DbSet<MedicalRecord> MedicalRecords { get; }
    public DbSet<Examination> Examinations { get; }
    public DbSet<Medication> Medications { get; }
    public DbSet<Prescription> Prescriptions { get; }

    public MedicalDbContext(string connectionString) : base(connectionString)
    {
        Patients = new DbSet<Patient>(this);
        MedicalRecords = new DbSet<MedicalRecord>(this);
        Examinations = new DbSet<Examination>(this);
        Medications = new DbSet<Medication>(this);
        Prescriptions = new DbSet<Prescription>(this);
    }
}