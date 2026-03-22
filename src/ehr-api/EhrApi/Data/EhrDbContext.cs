using Microsoft.EntityFrameworkCore;
using EhrApi.Models;

namespace EhrApi.Data;

public class EhrDbContext : DbContext
{
    public EhrDbContext(DbContextOptions<EhrDbContext> options) : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<LabResult> LabResults => Set<LabResult>();
    public DbSet<Allergy> Allergies => Set<Allergy>();
    public DbSet<Vitals> Vitals => Set<Vitals>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Patient indexes
        modelBuilder.Entity<Patient>()
            .HasIndex(p => new { p.LastName, p.FirstName });
        modelBuilder.Entity<Patient>()
            .HasIndex(p => p.InsuranceId);

        // Appointment relationships
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId);
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Provider)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.ProviderId);
        modelBuilder.Entity<Appointment>()
            .HasIndex(a => a.ScheduledAt);

        // MedicalRecord relationships
        modelBuilder.Entity<MedicalRecord>()
            .HasOne(m => m.Patient)
            .WithMany(p => p.MedicalRecords)
            .HasForeignKey(m => m.PatientId);
        modelBuilder.Entity<MedicalRecord>()
            .HasOne(m => m.Provider)
            .WithMany(p => p.MedicalRecords)
            .HasForeignKey(m => m.ProviderId);

        // Prescription relationships
        modelBuilder.Entity<Prescription>()
            .HasOne(rx => rx.Patient)
            .WithMany(p => p.Prescriptions)
            .HasForeignKey(rx => rx.PatientId);
        modelBuilder.Entity<Prescription>()
            .HasOne(rx => rx.Provider)
            .WithMany(p => p.Prescriptions)
            .HasForeignKey(rx => rx.ProviderId);

        // LabResult
        modelBuilder.Entity<LabResult>()
            .HasOne(l => l.Patient)
            .WithMany(p => p.LabResults)
            .HasForeignKey(l => l.PatientId);

        // Allergy
        modelBuilder.Entity<Allergy>()
            .HasOne(a => a.Patient)
            .WithMany(p => p.Allergies)
            .HasForeignKey(a => a.PatientId);

        // Vitals
        modelBuilder.Entity<Vitals>()
            .HasOne(v => v.Patient)
            .WithMany(p => p.Vitals)
            .HasForeignKey(v => v.PatientId);

        // Seed data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var utcNow = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var today = new DateTime(2026, 3, 22, 0, 0, 0, DateTimeKind.Utc);

        // ── Providers (8) ──
        modelBuilder.Entity<Provider>().HasData(
            new Provider { Id = 1, FirstName = "Sarah", LastName = "Chen", Specialty = "Family Medicine", LicenseNumber = "FM-2024-001", Phone = "416-555-0101", Email = "s.chen@ehrhealth.ca", Department = "Primary Care", CreatedAt = utcNow },
            new Provider { Id = 2, FirstName = "James", LastName = "Wilson", Specialty = "Cardiology", LicenseNumber = "CD-2024-002", Phone = "416-555-0102", Email = "j.wilson@ehrhealth.ca", Department = "Cardiology", CreatedAt = utcNow },
            new Provider { Id = 3, FirstName = "Maria", LastName = "Garcia", Specialty = "Pediatrics", LicenseNumber = "PD-2024-003", Phone = "416-555-0103", Email = "m.garcia@ehrhealth.ca", Department = "Pediatrics", CreatedAt = utcNow },
            new Provider { Id = 4, FirstName = "David", LastName = "Kim", Specialty = "Orthopedics", LicenseNumber = "OR-2024-004", Phone = "416-555-0104", Email = "d.kim@ehrhealth.ca", Department = "Orthopedics", CreatedAt = utcNow },
            new Provider { Id = 5, FirstName = "Lisa", LastName = "Patel", Specialty = "Dermatology", LicenseNumber = "DM-2024-005", Phone = "416-555-0105", Email = "l.patel@ehrhealth.ca", Department = "Dermatology", CreatedAt = utcNow },
            new Provider { Id = 6, FirstName = "Robert", LastName = "Thompson", Specialty = "Neurology", LicenseNumber = "NR-2024-006", Phone = "416-555-0106", Email = "r.thompson@ehrhealth.ca", Department = "Neurology", CreatedAt = utcNow },
            new Provider { Id = 7, FirstName = "Angela", LastName = "Russo", Specialty = "Oncology", LicenseNumber = "ON-2024-007", Phone = "416-555-0107", Email = "a.russo@ehrhealth.ca", Department = "Oncology", CreatedAt = utcNow },
            new Provider { Id = 8, FirstName = "Kevin", LastName = "Nguyen", Specialty = "Emergency Medicine", LicenseNumber = "EM-2024-008", Phone = "416-555-0108", Email = "k.nguyen@ehrhealth.ca", Department = "Emergency", CreatedAt = utcNow }
        );

        // ── Patients (15) ──
        modelBuilder.Entity<Patient>().HasData(
            new Patient { Id = 1, FirstName = "John", LastName = "Doe", DateOfBirth = new DateTime(1985, 3, 15, 0, 0, 0, DateTimeKind.Utc), Gender = "Male", Phone = "416-555-1001", Email = "john.doe@email.com", Address = "123 Main St, Toronto, ON", InsuranceId = "INS-001", BloodType = "O+", EmergencyContactName = "Jane Doe", EmergencyContactPhone = "416-555-1002", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Patient { Id = 2, FirstName = "Emily", LastName = "Smith", DateOfBirth = new DateTime(1992, 7, 22, 0, 0, 0, DateTimeKind.Utc), Gender = "Female", Phone = "416-555-1003", Email = "emily.smith@email.com", Address = "456 Oak Ave, Toronto, ON", InsuranceId = "INS-002", BloodType = "A+", EmergencyContactName = "Bob Smith", EmergencyContactPhone = "416-555-1004", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Patient { Id = 3, FirstName = "Michael", LastName = "Johnson", DateOfBirth = new DateTime(1978, 11, 8, 0, 0, 0, DateTimeKind.Utc), Gender = "Male", Phone = "416-555-1005", Email = "m.johnson@email.com", Address = "789 Pine Rd, Mississauga, ON", InsuranceId = "INS-003", BloodType = "B-", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Patient { Id = 4, FirstName = "Sophia", LastName = "Brown", DateOfBirth = new DateTime(2001, 5, 12, 0, 0, 0, DateTimeKind.Utc), Gender = "Female", Phone = "416-555-1006", Email = "sophia.b@email.com", Address = "22 Queen St W, Toronto, ON", InsuranceId = "INS-004", BloodType = "AB+", EmergencyContactName = "Linda Brown", EmergencyContactPhone = "416-555-1007", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Patient { Id = 5, FirstName = "William", LastName = "Taylor", DateOfBirth = new DateTime(1965, 9, 3, 0, 0, 0, DateTimeKind.Utc), Gender = "Male", Phone = "905-555-2001", Email = "w.taylor@email.com", Address = "50 Lakeshore Blvd, Oakville, ON", InsuranceId = "INS-005", BloodType = "A-", EmergencyContactName = "Patricia Taylor", EmergencyContactPhone = "905-555-2002", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Patient { Id = 6, FirstName = "Olivia", LastName = "Martinez", DateOfBirth = new DateTime(1998, 12, 28, 0, 0, 0, DateTimeKind.Utc), Gender = "Female", Phone = "647-555-3001", Email = "olivia.m@email.com", Address = "88 Dundas St E, Toronto, ON", InsuranceId = "INS-006", BloodType = "O-", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Patient { Id = 7, FirstName = "Liam", LastName = "Anderson", DateOfBirth = new DateTime(1970, 1, 19, 0, 0, 0, DateTimeKind.Utc), Gender = "Male", Phone = "416-555-4001", Email = "liam.a@email.com", Address = "15 Yonge St, Toronto, ON", InsuranceId = "INS-007", BloodType = "B+", EmergencyContactName = "Carol Anderson", EmergencyContactPhone = "416-555-4002", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Patient { Id = 8, FirstName = "Ava", LastName = "Thomas", DateOfBirth = new DateTime(2010, 8, 5, 0, 0, 0, DateTimeKind.Utc), Gender = "Female", Phone = "416-555-5001", Email = "ava.parent@email.com", Address = "200 Bloor St W, Toronto, ON", InsuranceId = "INS-008", BloodType = "A+", EmergencyContactName = "Mark Thomas", EmergencyContactPhone = "416-555-5002", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Patient { Id = 9, FirstName = "Noah", LastName = "Jackson", DateOfBirth = new DateTime(1990, 4, 17, 0, 0, 0, DateTimeKind.Utc), Gender = "Male", Phone = "905-555-6001", Email = "noah.j@email.com", Address = "33 King St, Hamilton, ON", InsuranceId = "INS-009", BloodType = "O+", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Patient { Id = 10, FirstName = "Isabella", LastName = "White", DateOfBirth = new DateTime(1955, 2, 14, 0, 0, 0, DateTimeKind.Utc), Gender = "Female", Phone = "416-555-7001", Email = "isabella.w@email.com", Address = "77 College St, Toronto, ON", InsuranceId = "INS-010", BloodType = "AB-", EmergencyContactName = "George White", EmergencyContactPhone = "416-555-7002", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Patient { Id = 11, FirstName = "Ethan", LastName = "Harris", DateOfBirth = new DateTime(1988, 6, 30, 0, 0, 0, DateTimeKind.Utc), Gender = "Male", Phone = "647-555-8001", Email = "ethan.h@email.com", Address = "5 Eglinton Ave, Toronto, ON", InsuranceId = "INS-011", BloodType = "A+", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Patient { Id = 12, FirstName = "Mia", LastName = "Clark", DateOfBirth = new DateTime(1975, 10, 22, 0, 0, 0, DateTimeKind.Utc), Gender = "Female", Phone = "416-555-9001", Email = "mia.c@email.com", Address = "140 Bay St, Toronto, ON", InsuranceId = "INS-012", BloodType = "O+", EmergencyContactName = "David Clark", EmergencyContactPhone = "416-555-9002", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Patient { Id = 13, FirstName = "Alexander", LastName = "Lewis", DateOfBirth = new DateTime(2005, 3, 8, 0, 0, 0, DateTimeKind.Utc), Gender = "Male", Phone = "905-555-1101", Email = "alex.l.parent@email.com", Address = "60 Hurontario St, Brampton, ON", InsuranceId = "INS-013", BloodType = "B+", EmergencyContactName = "Susan Lewis", EmergencyContactPhone = "905-555-1102", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Patient { Id = 14, FirstName = "Charlotte", LastName = "Walker", DateOfBirth = new DateTime(1982, 7, 11, 0, 0, 0, DateTimeKind.Utc), Gender = "Female", Phone = "416-555-1201", Email = "charlotte.w@email.com", Address = "95 Spadina Ave, Toronto, ON", InsuranceId = "INS-014", BloodType = "A-", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Patient { Id = 15, FirstName = "Benjamin", LastName = "Hall", DateOfBirth = new DateTime(1960, 11, 25, 0, 0, 0, DateTimeKind.Utc), Gender = "Male", Phone = "905-555-1301", Email = "ben.hall@email.com", Address = "180 Dundas St W, London, ON", InsuranceId = "INS-015", BloodType = "O-", EmergencyContactName = "Margaret Hall", EmergencyContactPhone = "905-555-1302", CreatedAt = utcNow, UpdatedAt = utcNow }
        );

        // ── Appointments (20) ── including today's appointments
        modelBuilder.Entity<Appointment>().HasData(
            // Today's appointments
            new Appointment { Id = 1, PatientId = 1, ProviderId = 1, ScheduledAt = today.AddHours(9), DurationMinutes = 30, Status = "Completed", Type = "General", Reason = "Annual physical exam", Notes = "Patient appears healthy overall", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Appointment { Id = 2, PatientId = 2, ProviderId = 2, ScheduledAt = today.AddHours(9).AddMinutes(30), DurationMinutes = 45, Status = "Completed", Type = "FollowUp", Reason = "Cardiac follow-up after echocardiogram", Notes = "Results reviewed with patient", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Appointment { Id = 3, PatientId = 5, ProviderId = 1, ScheduledAt = today.AddHours(10), DurationMinutes = 30, Status = "InProgress", Type = "General", Reason = "Blood pressure management review", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Appointment { Id = 4, PatientId = 8, ProviderId = 3, ScheduledAt = today.AddHours(10).AddMinutes(30), DurationMinutes = 30, Status = "CheckedIn", Type = "General", Reason = "Growth checkup - pediatric visit", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Appointment { Id = 5, PatientId = 11, ProviderId = 4, ScheduledAt = today.AddHours(11), DurationMinutes = 45, Status = "Scheduled", Type = "Specialist", Reason = "Knee pain evaluation", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Appointment { Id = 6, PatientId = 6, ProviderId = 5, ScheduledAt = today.AddHours(13), DurationMinutes = 30, Status = "Scheduled", Type = "Specialist", Reason = "Skin rash follow-up", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Appointment { Id = 7, PatientId = 10, ProviderId = 6, ScheduledAt = today.AddHours(14), DurationMinutes = 60, Status = "Scheduled", Type = "Specialist", Reason = "Neurological assessment - headaches", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Appointment { Id = 8, PatientId = 15, ProviderId = 2, ScheduledAt = today.AddHours(15), DurationMinutes = 30, Status = "Scheduled", Type = "FollowUp", Reason = "Post-stent implant follow-up", CreatedAt = utcNow, UpdatedAt = utcNow },
            // Past appointments
            new Appointment { Id = 9, PatientId = 1, ProviderId = 2, ScheduledAt = today.AddDays(-14).AddHours(10), DurationMinutes = 45, Status = "Completed", Type = "Specialist", Reason = "Chest pain evaluation", Notes = "ECG normal, stress test ordered", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Appointment { Id = 10, PatientId = 3, ProviderId = 1, ScheduledAt = today.AddDays(-10).AddHours(9), DurationMinutes = 30, Status = "Completed", Type = "General", Reason = "Diabetes management", Notes = "HbA1c slightly elevated, adjusted metformin", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Appointment { Id = 11, PatientId = 4, ProviderId = 5, ScheduledAt = today.AddDays(-7).AddHours(11), DurationMinutes = 30, Status = "Completed", Type = "Specialist", Reason = "Acne treatment", Notes = "Prescribed topical retinoid", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Appointment { Id = 12, PatientId = 7, ProviderId = 6, ScheduledAt = today.AddDays(-5).AddHours(14), DurationMinutes = 60, Status = "Completed", Type = "Specialist", Reason = "Migraine evaluation", Notes = "MRI ordered, started preventive medication", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Appointment { Id = 13, PatientId = 9, ProviderId = 1, ScheduledAt = today.AddDays(-3).AddHours(9).AddMinutes(30), DurationMinutes = 30, Status = "Completed", Type = "General", Reason = "Flu symptoms", Notes = "Viral infection, rest and fluids recommended", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Appointment { Id = 14, PatientId = 12, ProviderId = 7, ScheduledAt = today.AddDays(-2).AddHours(10), DurationMinutes = 60, Status = "Completed", Type = "Specialist", Reason = "Breast cancer screening follow-up", Notes = "Mammogram results normal", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Appointment { Id = 15, PatientId = 14, ProviderId = 1, ScheduledAt = today.AddDays(-1).AddHours(15), DurationMinutes = 30, Status = "Completed", Type = "General", Reason = "Anxiety and insomnia", Notes = "Referred to psychiatrist, sleep hygiene discussed", CreatedAt = utcNow, UpdatedAt = utcNow },
            // Cancelled / No-show
            new Appointment { Id = 16, PatientId = 6, ProviderId = 1, ScheduledAt = today.AddDays(-4).AddHours(10), DurationMinutes = 30, Status = "Cancelled", Type = "General", Reason = "Sore throat", Notes = "Patient cancelled due to scheduling conflict", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Appointment { Id = 17, PatientId = 13, ProviderId = 3, ScheduledAt = today.AddDays(-6).AddHours(11), DurationMinutes = 30, Status = "NoShow", Type = "General", Reason = "Sports physical", CreatedAt = utcNow, UpdatedAt = utcNow },
            // Future appointments
            new Appointment { Id = 18, PatientId = 3, ProviderId = 2, ScheduledAt = today.AddDays(3).AddHours(10), DurationMinutes = 45, Status = "Scheduled", Type = "FollowUp", Reason = "Cardiac stress test results review", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Appointment { Id = 19, PatientId = 7, ProviderId = 6, ScheduledAt = today.AddDays(7).AddHours(14), DurationMinutes = 60, Status = "Scheduled", Type = "FollowUp", Reason = "MRI results review - migraines", CreatedAt = utcNow, UpdatedAt = utcNow },
            new Appointment { Id = 20, PatientId = 9, ProviderId = 4, ScheduledAt = today.AddDays(10).AddHours(11), DurationMinutes = 45, Status = "Scheduled", Type = "Specialist", Reason = "Lower back pain evaluation", CreatedAt = utcNow, UpdatedAt = utcNow }
        );

        // ── Medical Records (15) ──
        modelBuilder.Entity<MedicalRecord>().HasData(
            new MedicalRecord { Id = 1, PatientId = 1, ProviderId = 1, EncounterDate = today, EncounterType = "Office Visit", ChiefComplaint = "Annual physical exam", Diagnosis = "Healthy adult, mild seasonal allergies", DiagnosisCode = "Z00.00", TreatmentPlan = "Continue current allergy medication. Return in 12 months.", ClinicalNotes = "Patient is in good health. BMI 24.5. All vitals within normal limits.", FollowUpInstructions = "Annual follow-up in 1 year", CreatedAt = utcNow, UpdatedAt = utcNow },
            new MedicalRecord { Id = 2, PatientId = 2, ProviderId = 2, EncounterDate = today, EncounterType = "Office Visit", ChiefComplaint = "Follow-up for heart palpitations", Diagnosis = "Benign premature ventricular contractions", DiagnosisCode = "I49.3", TreatmentPlan = "Monitor with Holter for 48 hours. Reduce caffeine intake.", ClinicalNotes = "Echo results normal. EF 60%. No structural abnormalities.", FollowUpInstructions = "Return in 2 weeks with Holter monitor results", CreatedAt = utcNow, UpdatedAt = utcNow },
            new MedicalRecord { Id = 3, PatientId = 3, ProviderId = 1, EncounterDate = today.AddDays(-10), EncounterType = "Office Visit", ChiefComplaint = "Diabetes management", Diagnosis = "Type 2 Diabetes Mellitus, uncontrolled", DiagnosisCode = "E11.65", TreatmentPlan = "Increase Metformin to 1000mg BID. Dietary counseling referral.", ClinicalNotes = "HbA1c 7.8%. Fasting glucose 145 mg/dL. Peripheral neuropathy screening negative.", FollowUpInstructions = "Repeat HbA1c in 3 months. Follow up in 4 weeks.", CreatedAt = utcNow, UpdatedAt = utcNow },
            new MedicalRecord { Id = 4, PatientId = 5, ProviderId = 2, EncounterDate = today.AddDays(-30), EncounterType = "Office Visit", ChiefComplaint = "Hypertension management", Diagnosis = "Essential hypertension, Stage 2", DiagnosisCode = "I10", TreatmentPlan = "Started Lisinopril 10mg daily. Low sodium diet recommended.", ClinicalNotes = "BP 158/95 mmHg. BMI 28.3. Kidney function tests normal. Started DASH diet counseling.", FollowUpInstructions = "BP check in 2 weeks. Full follow-up in 4 weeks.", CreatedAt = utcNow, UpdatedAt = utcNow },
            new MedicalRecord { Id = 5, PatientId = 4, ProviderId = 5, EncounterDate = today.AddDays(-7), EncounterType = "Office Visit", ChiefComplaint = "Persistent acne", Diagnosis = "Acne vulgaris, moderate", DiagnosisCode = "L70.0", TreatmentPlan = "Topical tretinoin 0.025% nightly. Benzoyl peroxide 5% wash AM.", ClinicalNotes = "Moderate inflammatory acne on face and upper back. No scarring noted.", FollowUpInstructions = "Follow up in 6 weeks to assess response", CreatedAt = utcNow, UpdatedAt = utcNow },
            new MedicalRecord { Id = 6, PatientId = 7, ProviderId = 6, EncounterDate = today.AddDays(-5), EncounterType = "Office Visit", ChiefComplaint = "Recurrent migraines", Diagnosis = "Migraine without aura, chronic", DiagnosisCode = "G43.709", TreatmentPlan = "Sumatriptan 50mg PRN for acute attacks. Topiramate 25mg daily for prevention. MRI brain ordered.", ClinicalNotes = "Patient reports 4-5 migraines per month, lasting 6-12 hours. Photophobia and nausea present. Neurological exam normal.", FollowUpInstructions = "MRI brain within 2 weeks. Follow up after results.", CreatedAt = utcNow, UpdatedAt = utcNow },
            new MedicalRecord { Id = 7, PatientId = 8, ProviderId = 3, EncounterDate = today.AddDays(-60), EncounterType = "Office Visit", ChiefComplaint = "Well-child visit", Diagnosis = "Routine child health examination", DiagnosisCode = "Z00.129", TreatmentPlan = "Vaccines up to date. Next visit in 6 months.", ClinicalNotes = "Growth tracking: 75th percentile height, 60th percentile weight. Development milestones on track.", FollowUpInstructions = "Next well-child visit in 6 months", CreatedAt = utcNow, UpdatedAt = utcNow },
            new MedicalRecord { Id = 8, PatientId = 9, ProviderId = 1, EncounterDate = today.AddDays(-3), EncounterType = "Office Visit", ChiefComplaint = "Fever, cough, body aches for 3 days", Diagnosis = "Acute upper respiratory infection", DiagnosisCode = "J06.9", TreatmentPlan = "Symptomatic treatment. Acetaminophen 500mg q6h PRN. Rest, fluids.", ClinicalNotes = "Temp 100.8F. Mild pharyngeal erythema. Lungs clear bilaterally. Rapid strep negative.", FollowUpInstructions = "Return if symptoms worsen or persist beyond 7 days", CreatedAt = utcNow, UpdatedAt = utcNow },
            new MedicalRecord { Id = 9, PatientId = 10, ProviderId = 6, EncounterDate = today.AddDays(-20), EncounterType = "Office Visit", ChiefComplaint = "Memory concerns and headaches", Diagnosis = "Mild cognitive impairment", DiagnosisCode = "G31.84", TreatmentPlan = "Neuropsychological testing ordered. Donepezil 5mg daily. Brain MRI scheduled.", ClinicalNotes = "MMSE score 24/30. Short-term memory deficits noted. Cranial nerves intact. No focal deficits.", FollowUpInstructions = "Complete MRI and neuropsych testing. Follow up in 4 weeks.", CreatedAt = utcNow, UpdatedAt = utcNow },
            new MedicalRecord { Id = 10, PatientId = 12, ProviderId = 7, EncounterDate = today.AddDays(-2), EncounterType = "Office Visit", ChiefComplaint = "Breast cancer screening follow-up", Diagnosis = "Screening mammogram - normal", DiagnosisCode = "Z12.31", TreatmentPlan = "Continue annual mammography. No further intervention needed.", ClinicalNotes = "Bilateral mammogram BI-RADS 1. No suspicious findings. Patient reassured.", FollowUpInstructions = "Annual mammogram in 12 months", CreatedAt = utcNow, UpdatedAt = utcNow },
            new MedicalRecord { Id = 11, PatientId = 11, ProviderId = 4, EncounterDate = today.AddDays(-15), EncounterType = "Office Visit", ChiefComplaint = "Left knee pain for 3 weeks", Diagnosis = "Patellar tendinitis", DiagnosisCode = "M76.50", TreatmentPlan = "Physical therapy 2x/week for 6 weeks. Ibuprofen 400mg TID with food. Ice therapy.", ClinicalNotes = "Tenderness over patellar tendon. Full ROM with pain at terminal extension. No effusion. X-ray unremarkable.", FollowUpInstructions = "Follow up in 6 weeks. Sooner if worsening.", CreatedAt = utcNow, UpdatedAt = utcNow },
            new MedicalRecord { Id = 12, PatientId = 14, ProviderId = 1, EncounterDate = today.AddDays(-1), EncounterType = "Telehealth", ChiefComplaint = "Anxiety and difficulty sleeping", Diagnosis = "Generalized anxiety disorder with insomnia", DiagnosisCode = "F41.1", TreatmentPlan = "Sleep hygiene education. Melatonin 3mg at bedtime. Psychiatric referral for CBT.", ClinicalNotes = "Patient reports persistent worry, difficulty falling asleep for past 2 months. PHQ-9 score 8. GAD-7 score 12.", FollowUpInstructions = "Psychiatric evaluation within 2 weeks. Follow up in 4 weeks.", CreatedAt = utcNow, UpdatedAt = utcNow },
            new MedicalRecord { Id = 13, PatientId = 15, ProviderId = 2, EncounterDate = today.AddDays(-45), EncounterType = "Hospitalization", ChiefComplaint = "Acute chest pain radiating to left arm", Diagnosis = "Acute myocardial infarction - STEMI", DiagnosisCode = "I21.0", TreatmentPlan = "Emergency PCI with drug-eluting stent. Dual antiplatelet therapy. Cardiac rehabilitation.", ClinicalNotes = "Troponin elevated at 2.5 ng/mL. ECG showed ST elevation in leads II, III, aVF. Cath lab activated. Successful PCI to RCA.", FollowUpInstructions = "Cardiac rehab 3x/week. Follow up in 2 weeks. Take all medications as prescribed.", CreatedAt = utcNow, UpdatedAt = utcNow },
            new MedicalRecord { Id = 14, PatientId = 6, ProviderId = 5, EncounterDate = today.AddDays(-21), EncounterType = "Office Visit", ChiefComplaint = "Itchy rash on arms and legs", Diagnosis = "Contact dermatitis", DiagnosisCode = "L25.9", TreatmentPlan = "Triamcinolone cream 0.1% BID to affected areas. Avoid known irritants.", ClinicalNotes = "Erythematous papular rash on bilateral forearms and lower legs. No vesicles. Likely contact irritant.", FollowUpInstructions = "Follow up in 2 weeks if not improving", CreatedAt = utcNow, UpdatedAt = utcNow },
            new MedicalRecord { Id = 15, PatientId = 13, ProviderId = 3, EncounterDate = today.AddDays(-30), EncounterType = "Office Visit", ChiefComplaint = "Pre-sports physical", Diagnosis = "Healthy adolescent", DiagnosisCode = "Z02.5", TreatmentPlan = "Cleared for sports participation. No restrictions.", ClinicalNotes = "No murmurs, no musculoskeletal issues. Normal vision screening. BMI appropriate for age.", FollowUpInstructions = "Annual physical in 1 year", CreatedAt = utcNow, UpdatedAt = utcNow }
        );

        // ── Prescriptions (18) ──
        modelBuilder.Entity<Prescription>().HasData(
            new Prescription { Id = 1, PatientId = 1, ProviderId = 1, MedicationName = "Cetirizine (Zyrtec)", Dosage = "10mg", Frequency = "Once daily", Route = "Oral", StartDate = today.AddDays(-90), Refills = 5, Status = "Active", Instructions = "Take once daily for allergies. May cause drowsiness.", Pharmacy = "Shoppers Drug Mart - 123 Main St", CreatedAt = utcNow },
            new Prescription { Id = 2, PatientId = 3, ProviderId = 1, MedicationName = "Metformin", Dosage = "1000mg", Frequency = "Twice daily", Route = "Oral", StartDate = today.AddDays(-10), Refills = 3, Status = "Active", Instructions = "Take with meals to reduce GI side effects. Monitor blood sugar regularly.", Pharmacy = "Rexall Pharmacy - 789 Pine Rd", CreatedAt = utcNow },
            new Prescription { Id = 3, PatientId = 5, ProviderId = 2, MedicationName = "Lisinopril", Dosage = "10mg", Frequency = "Once daily", Route = "Oral", StartDate = today.AddDays(-30), Refills = 5, Status = "Active", Instructions = "Take in the morning. Monitor blood pressure. Report persistent cough.", Pharmacy = "Costco Pharmacy - Oakville", CreatedAt = utcNow },
            new Prescription { Id = 4, PatientId = 4, ProviderId = 5, MedicationName = "Tretinoin Cream", Dosage = "0.025%", Frequency = "Once daily at bedtime", Route = "Topical", StartDate = today.AddDays(-7), Refills = 2, Status = "Active", Instructions = "Apply thin layer to clean, dry skin at night. Use sunscreen during the day.", Pharmacy = "Shoppers Drug Mart - Queen St", CreatedAt = utcNow },
            new Prescription { Id = 5, PatientId = 7, ProviderId = 6, MedicationName = "Sumatriptan", Dosage = "50mg", Frequency = "As needed for migraine", Route = "Oral", StartDate = today.AddDays(-5), Refills = 3, Status = "Active", Instructions = "Take at onset of migraine. May repeat after 2 hours. Max 200mg/day.", Pharmacy = "Pharmasave - 15 Yonge St", CreatedAt = utcNow },
            new Prescription { Id = 6, PatientId = 7, ProviderId = 6, MedicationName = "Topiramate", Dosage = "25mg", Frequency = "Once daily", Route = "Oral", StartDate = today.AddDays(-5), Refills = 5, Status = "Active", Instructions = "Take at bedtime. Increase to 50mg after 2 weeks if tolerated.", Pharmacy = "Pharmasave - 15 Yonge St", CreatedAt = utcNow },
            new Prescription { Id = 7, PatientId = 10, ProviderId = 6, MedicationName = "Donepezil (Aricept)", Dosage = "5mg", Frequency = "Once daily", Route = "Oral", StartDate = today.AddDays(-20), Refills = 5, Status = "Active", Instructions = "Take at bedtime. May increase to 10mg after 4-6 weeks.", Pharmacy = "Shoppers Drug Mart - College St", CreatedAt = utcNow },
            new Prescription { Id = 8, PatientId = 11, ProviderId = 4, MedicationName = "Ibuprofen", Dosage = "400mg", Frequency = "Three times daily", Route = "Oral", StartDate = today.AddDays(-15), EndDate = today.AddDays(27), Refills = 0, Status = "Active", Instructions = "Take with food. Discontinue if stomach upset occurs.", Pharmacy = "Rexall Pharmacy - Eglinton Ave", CreatedAt = utcNow },
            new Prescription { Id = 9, PatientId = 15, ProviderId = 2, MedicationName = "Aspirin", Dosage = "81mg", Frequency = "Once daily", Route = "Oral", StartDate = today.AddDays(-45), Refills = 11, Status = "Active", Instructions = "Take daily. Do not stop without consulting doctor.", Pharmacy = "London Drugs - Dundas St", CreatedAt = utcNow },
            new Prescription { Id = 10, PatientId = 15, ProviderId = 2, MedicationName = "Clopidogrel (Plavix)", Dosage = "75mg", Frequency = "Once daily", Route = "Oral", StartDate = today.AddDays(-45), EndDate = today.AddDays(320), Refills = 11, Status = "Active", Instructions = "Take daily for 12 months post-stent. Do not stop without consulting cardiologist.", Pharmacy = "London Drugs - Dundas St", CreatedAt = utcNow },
            new Prescription { Id = 11, PatientId = 15, ProviderId = 2, MedicationName = "Atorvastatin (Lipitor)", Dosage = "80mg", Frequency = "Once daily at bedtime", Route = "Oral", StartDate = today.AddDays(-45), Refills = 11, Status = "Active", Instructions = "Take at bedtime. Report muscle pain or weakness.", Pharmacy = "London Drugs - Dundas St", CreatedAt = utcNow },
            new Prescription { Id = 12, PatientId = 6, ProviderId = 5, MedicationName = "Triamcinolone Cream", Dosage = "0.1%", Frequency = "Twice daily", Route = "Topical", StartDate = today.AddDays(-21), EndDate = today.AddDays(-7), Refills = 0, Status = "Completed", Instructions = "Apply thin layer to rash areas. Do not use on face.", Pharmacy = "Shoppers Drug Mart - Dundas St", CreatedAt = utcNow },
            new Prescription { Id = 13, PatientId = 14, ProviderId = 1, MedicationName = "Melatonin", Dosage = "3mg", Frequency = "Once daily at bedtime", Route = "Oral", StartDate = today.AddDays(-1), Refills = 2, Status = "Active", Instructions = "Take 30 minutes before bedtime. Maintain regular sleep schedule.", Pharmacy = "Shoppers Drug Mart - Spadina Ave", CreatedAt = utcNow },
            new Prescription { Id = 14, PatientId = 9, ProviderId = 1, MedicationName = "Acetaminophen (Tylenol)", Dosage = "500mg", Frequency = "Every 6 hours as needed", Route = "Oral", StartDate = today.AddDays(-3), EndDate = today.AddDays(4), Refills = 0, Status = "Active", Instructions = "Take as needed for fever/pain. Max 4000mg/day.", Pharmacy = "Rexall Pharmacy - King St", CreatedAt = utcNow },
            new Prescription { Id = 15, PatientId = 2, ProviderId = 2, MedicationName = "Metoprolol", Dosage = "25mg", Frequency = "Twice daily", Route = "Oral", StartDate = today.AddDays(-60), Refills = 5, Status = "Active", Instructions = "Take with meals. Do not stop abruptly.", Pharmacy = "Shoppers Drug Mart - Oak Ave", CreatedAt = utcNow },
            new Prescription { Id = 16, PatientId = 5, ProviderId = 1, MedicationName = "Atorvastatin (Lipitor)", Dosage = "20mg", Frequency = "Once daily at bedtime", Route = "Oral", StartDate = today.AddDays(-90), Refills = 5, Status = "Active", Instructions = "Take at bedtime. Annual lipid panel required.", Pharmacy = "Costco Pharmacy - Oakville", CreatedAt = utcNow },
            new Prescription { Id = 17, PatientId = 3, ProviderId = 1, MedicationName = "Glipizide", Dosage = "5mg", Frequency = "Once daily before breakfast", Route = "Oral", StartDate = today.AddDays(-180), Refills = 5, Status = "Active", Instructions = "Take 30 minutes before breakfast. Monitor for low blood sugar.", Pharmacy = "Rexall Pharmacy - 789 Pine Rd", CreatedAt = utcNow },
            new Prescription { Id = 18, PatientId = 1, ProviderId = 2, MedicationName = "Aspirin", Dosage = "81mg", Frequency = "Once daily", Route = "Oral", StartDate = today.AddDays(-180), EndDate = today.AddDays(-30), Refills = 0, Status = "Completed", Instructions = "Low-dose aspirin for cardiac prevention. Discontinued per provider review.", Pharmacy = "Shoppers Drug Mart - 123 Main St", CreatedAt = utcNow }
        );

        // ── Lab Results (20) ──
        modelBuilder.Entity<LabResult>().HasData(
            new LabResult { Id = 1, PatientId = 1, OrderedByProviderId = 1, TestName = "Complete Blood Count (CBC)", TestCode = "58410-2", Result = "WBC 7.2, RBC 4.8, Hgb 14.5, Plt 250", Unit = "various", ReferenceRange = "WBC 4.5-11.0", Status = "Completed", Flag = "Normal", OrderedAt = today.AddDays(-1), CompletedAt = today, Notes = "All values within normal limits" },
            new LabResult { Id = 2, PatientId = 1, OrderedByProviderId = 1, TestName = "Comprehensive Metabolic Panel", TestCode = "24323-8", Result = "Glucose 95, BUN 18, Creatinine 1.0, Na 140, K 4.2", Unit = "various", ReferenceRange = "Glucose 70-100", Status = "Completed", Flag = "Normal", OrderedAt = today.AddDays(-1), CompletedAt = today },
            new LabResult { Id = 3, PatientId = 1, OrderedByProviderId = 1, TestName = "Lipid Panel", TestCode = "24331-1", Result = "Total Chol 198, LDL 120, HDL 55, Triglycerides 115", Unit = "mg/dL", ReferenceRange = "Total <200", Status = "Completed", Flag = "Normal", OrderedAt = today.AddDays(-1), CompletedAt = today },
            new LabResult { Id = 4, PatientId = 3, OrderedByProviderId = 1, TestName = "Hemoglobin A1c", TestCode = "4548-4", Result = "7.8", Unit = "%", ReferenceRange = "<5.7 normal, 5.7-6.4 prediabetes", Status = "Completed", Flag = "High", OrderedAt = today.AddDays(-12), CompletedAt = today.AddDays(-10), Notes = "Elevated - indicates suboptimal diabetes control" },
            new LabResult { Id = 5, PatientId = 3, OrderedByProviderId = 1, TestName = "Fasting Blood Glucose", TestCode = "1558-6", Result = "145", Unit = "mg/dL", ReferenceRange = "70-100 mg/dL", Status = "Completed", Flag = "High", OrderedAt = today.AddDays(-12), CompletedAt = today.AddDays(-10) },
            new LabResult { Id = 6, PatientId = 5, OrderedByProviderId = 2, TestName = "Basic Metabolic Panel", TestCode = "51990-0", Result = "Glucose 102, BUN 22, Creatinine 1.1, Na 141, K 4.5", Unit = "various", ReferenceRange = "Creatinine 0.7-1.3", Status = "Completed", Flag = "Normal", OrderedAt = today.AddDays(-32), CompletedAt = today.AddDays(-30) },
            new LabResult { Id = 7, PatientId = 5, OrderedByProviderId = 2, TestName = "Lipid Panel", TestCode = "24331-1", Result = "Total Chol 245, LDL 165, HDL 42, Triglycerides 190", Unit = "mg/dL", ReferenceRange = "LDL <100 optimal", Status = "Completed", Flag = "High", OrderedAt = today.AddDays(-32), CompletedAt = today.AddDays(-30), Notes = "Elevated LDL and triglycerides. Statin therapy initiated." },
            new LabResult { Id = 8, PatientId = 2, OrderedByProviderId = 2, TestName = "Thyroid Stimulating Hormone (TSH)", TestCode = "3016-3", Result = "2.5", Unit = "mIU/L", ReferenceRange = "0.4-4.0 mIU/L", Status = "Completed", Flag = "Normal", OrderedAt = today.AddDays(-5), CompletedAt = today.AddDays(-3) },
            new LabResult { Id = 9, PatientId = 2, OrderedByProviderId = 2, TestName = "Troponin I", TestCode = "10839-9", Result = "0.02", Unit = "ng/mL", ReferenceRange = "<0.04 ng/mL", Status = "Completed", Flag = "Normal", OrderedAt = today.AddDays(-5), CompletedAt = today.AddDays(-5), Notes = "Ruled out acute MI" },
            new LabResult { Id = 10, PatientId = 15, OrderedByProviderId = 2, TestName = "Troponin I", TestCode = "10839-9", Result = "2.5", Unit = "ng/mL", ReferenceRange = "<0.04 ng/mL", Status = "Completed", Flag = "Critical", OrderedAt = today.AddDays(-45), CompletedAt = today.AddDays(-45), Notes = "Markedly elevated - consistent with acute MI" },
            new LabResult { Id = 11, PatientId = 15, OrderedByProviderId = 2, TestName = "Complete Blood Count (CBC)", TestCode = "58410-2", Result = "WBC 11.2, RBC 4.1, Hgb 12.8, Plt 310", Unit = "various", ReferenceRange = "WBC 4.5-11.0", Status = "Completed", Flag = "High", OrderedAt = today.AddDays(-45), CompletedAt = today.AddDays(-45), Notes = "Mild leukocytosis, likely stress response" },
            new LabResult { Id = 12, PatientId = 10, OrderedByProviderId = 6, TestName = "Vitamin B12", TestCode = "2132-9", Result = "180", Unit = "pg/mL", ReferenceRange = "200-900 pg/mL", Status = "Completed", Flag = "Low", OrderedAt = today.AddDays(-22), CompletedAt = today.AddDays(-20), Notes = "Low B12 may contribute to cognitive symptoms" },
            new LabResult { Id = 13, PatientId = 10, OrderedByProviderId = 6, TestName = "Thyroid Stimulating Hormone (TSH)", TestCode = "3016-3", Result = "3.8", Unit = "mIU/L", ReferenceRange = "0.4-4.0 mIU/L", Status = "Completed", Flag = "Normal", OrderedAt = today.AddDays(-22), CompletedAt = today.AddDays(-20) },
            new LabResult { Id = 14, PatientId = 9, OrderedByProviderId = 1, TestName = "Rapid Strep Test", TestCode = "6558-9", Result = "Negative", Unit = "Qualitative", ReferenceRange = "Negative", Status = "Completed", Flag = "Normal", OrderedAt = today.AddDays(-3), CompletedAt = today.AddDays(-3) },
            new LabResult { Id = 15, PatientId = 12, OrderedByProviderId = 7, TestName = "CA-125", TestCode = "10334-1", Result = "18", Unit = "U/mL", ReferenceRange = "<35 U/mL", Status = "Completed", Flag = "Normal", OrderedAt = today.AddDays(-5), CompletedAt = today.AddDays(-2) },
            new LabResult { Id = 16, PatientId = 11, OrderedByProviderId = 4, TestName = "C-Reactive Protein (CRP)", TestCode = "1988-5", Result = "0.8", Unit = "mg/L", ReferenceRange = "<3.0 mg/L", Status = "Completed", Flag = "Normal", OrderedAt = today.AddDays(-15), CompletedAt = today.AddDays(-14) },
            new LabResult { Id = 17, PatientId = 7, OrderedByProviderId = 6, TestName = "MRI Brain", TestCode = "30799-1", Status = "Pending", OrderedAt = today.AddDays(-5), Notes = "Ordered for chronic migraine evaluation" },
            new LabResult { Id = 18, PatientId = 10, OrderedByProviderId = 6, TestName = "MRI Brain with Contrast", TestCode = "30799-1", Status = "Pending", OrderedAt = today.AddDays(-3), Notes = "Ordered for cognitive impairment workup" },
            new LabResult { Id = 19, PatientId = 14, OrderedByProviderId = 1, TestName = "Thyroid Panel", TestCode = "24348-5", Status = "Pending", OrderedAt = today, Notes = "Rule out thyroid contribution to anxiety/insomnia" },
            new LabResult { Id = 20, PatientId = 5, OrderedByProviderId = 2, TestName = "Renal Function Panel", TestCode = "24362-6", Status = "Pending", OrderedAt = today, Notes = "Baseline renal function on ACE inhibitor therapy" }
        );

        // ── Allergies (12) ──
        modelBuilder.Entity<Allergy>().HasData(
            new Allergy { Id = 1, PatientId = 1, Allergen = "Penicillin", AllergyType = "Drug", Severity = "Severe", Reaction = "Anaphylaxis", ReportedAt = utcNow },
            new Allergy { Id = 2, PatientId = 2, Allergen = "Peanuts", AllergyType = "Food", Severity = "Life-Threatening", Reaction = "Anaphylaxis, swelling", ReportedAt = utcNow },
            new Allergy { Id = 3, PatientId = 1, Allergen = "Dust Mites", AllergyType = "Environmental", Severity = "Mild", Reaction = "Sneezing, congestion", ReportedAt = utcNow },
            new Allergy { Id = 4, PatientId = 5, Allergen = "Sulfa Drugs", AllergyType = "Drug", Severity = "Moderate", Reaction = "Skin rash, hives", ReportedAt = utcNow },
            new Allergy { Id = 5, PatientId = 7, Allergen = "Codeine", AllergyType = "Drug", Severity = "Severe", Reaction = "Respiratory depression, nausea", ReportedAt = utcNow },
            new Allergy { Id = 6, PatientId = 10, Allergen = "Latex", AllergyType = "Environmental", Severity = "Moderate", Reaction = "Contact dermatitis, itching", ReportedAt = utcNow },
            new Allergy { Id = 7, PatientId = 6, Allergen = "Shellfish", AllergyType = "Food", Severity = "Severe", Reaction = "Hives, throat swelling", ReportedAt = utcNow },
            new Allergy { Id = 8, PatientId = 8, Allergen = "Amoxicillin", AllergyType = "Drug", Severity = "Moderate", Reaction = "Rash, GI upset", ReportedAt = utcNow },
            new Allergy { Id = 9, PatientId = 12, Allergen = "Iodine Contrast Dye", AllergyType = "Drug", Severity = "Severe", Reaction = "Anaphylaxis", ReportedAt = utcNow },
            new Allergy { Id = 10, PatientId = 15, Allergen = "Aspirin", AllergyType = "Drug", Severity = "Mild", Reaction = "GI upset - tolerates low dose", IsActive = false, ReportedAt = utcNow },
            new Allergy { Id = 11, PatientId = 9, Allergen = "Pollen", AllergyType = "Environmental", Severity = "Mild", Reaction = "Seasonal rhinitis", ReportedAt = utcNow },
            new Allergy { Id = 12, PatientId = 4, Allergen = "Dairy", AllergyType = "Food", Severity = "Mild", Reaction = "Bloating, GI discomfort", ReportedAt = utcNow }
        );

        // ── Vitals (25) ──
        modelBuilder.Entity<Vitals>().HasData(
            // Today's vitals
            new Vitals { Id = 1, PatientId = 1, RecordedAt = today.AddHours(9), Temperature = 98.6m, HeartRate = 72, SystolicBp = 122, DiastolicBp = 78, RespiratoryRate = 16, OxygenSaturation = 99m, Weight = 175m, Height = 70m, Notes = "Annual physical" },
            new Vitals { Id = 2, PatientId = 2, RecordedAt = today.AddHours(9).AddMinutes(30), Temperature = 98.4m, HeartRate = 82, SystolicBp = 118, DiastolicBp = 74, RespiratoryRate = 18, OxygenSaturation = 98m, Weight = 135m, Height = 64m },
            new Vitals { Id = 3, PatientId = 5, RecordedAt = today.AddHours(10), Temperature = 98.2m, HeartRate = 78, SystolicBp = 148, DiastolicBp = 92, RespiratoryRate = 16, OxygenSaturation = 97m, Weight = 210m, Height = 72m, Notes = "BP still elevated despite medication" },
            new Vitals { Id = 4, PatientId = 8, RecordedAt = today.AddHours(10).AddMinutes(30), Temperature = 98.8m, HeartRate = 88, SystolicBp = 100, DiastolicBp = 65, RespiratoryRate = 20, OxygenSaturation = 99m, Weight = 72m, Height = 54m, Notes = "Pediatric patient" },
            // Past vitals
            new Vitals { Id = 5, PatientId = 3, RecordedAt = today.AddDays(-10).AddHours(9), Temperature = 98.4m, HeartRate = 76, SystolicBp = 134, DiastolicBp = 84, RespiratoryRate = 16, OxygenSaturation = 98m, Weight = 195m, Height = 68m },
            new Vitals { Id = 6, PatientId = 5, RecordedAt = today.AddDays(-30).AddHours(10), Temperature = 98.6m, HeartRate = 80, SystolicBp = 158, DiastolicBp = 95, RespiratoryRate = 18, OxygenSaturation = 97m, Weight = 215m, Height = 72m, Notes = "Initial visit - BP elevated" },
            new Vitals { Id = 7, PatientId = 4, RecordedAt = today.AddDays(-7).AddHours(11), Temperature = 98.4m, HeartRate = 70, SystolicBp = 112, DiastolicBp = 70, RespiratoryRate = 16, OxygenSaturation = 99m, Weight = 130m, Height = 65m },
            new Vitals { Id = 8, PatientId = 7, RecordedAt = today.AddDays(-5).AddHours(14), Temperature = 98.6m, HeartRate = 74, SystolicBp = 126, DiastolicBp = 80, RespiratoryRate = 16, OxygenSaturation = 98m, Weight = 185m, Height = 71m },
            new Vitals { Id = 9, PatientId = 9, RecordedAt = today.AddDays(-3).AddHours(9).AddMinutes(30), Temperature = 100.8m, HeartRate = 92, SystolicBp = 120, DiastolicBp = 76, RespiratoryRate = 20, OxygenSaturation = 97m, Weight = 170m, Height = 69m, Notes = "Febrile - URI" },
            new Vitals { Id = 10, PatientId = 10, RecordedAt = today.AddDays(-20).AddHours(14), Temperature = 97.8m, HeartRate = 68, SystolicBp = 142, DiastolicBp = 88, RespiratoryRate = 16, OxygenSaturation = 96m, Weight = 155m, Height = 63m },
            new Vitals { Id = 11, PatientId = 12, RecordedAt = today.AddDays(-2).AddHours(10), Temperature = 98.4m, HeartRate = 72, SystolicBp = 128, DiastolicBp = 82, RespiratoryRate = 16, OxygenSaturation = 98m, Weight = 160m, Height = 66m },
            new Vitals { Id = 12, PatientId = 11, RecordedAt = today.AddDays(-15).AddHours(11), Temperature = 98.6m, HeartRate = 68, SystolicBp = 118, DiastolicBp = 72, RespiratoryRate = 14, OxygenSaturation = 99m, Weight = 180m, Height = 73m },
            new Vitals { Id = 13, PatientId = 14, RecordedAt = today.AddDays(-1).AddHours(15), Temperature = 98.2m, HeartRate = 84, SystolicBp = 124, DiastolicBp = 78, RespiratoryRate = 18, OxygenSaturation = 99m, Weight = 140m, Height = 65m, Notes = "Mildly tachycardic - anxious" },
            new Vitals { Id = 14, PatientId = 15, RecordedAt = today.AddDays(-45).AddHours(8), Temperature = 98.8m, HeartRate = 105, SystolicBp = 165, DiastolicBp = 100, RespiratoryRate = 22, OxygenSaturation = 94m, Weight = 200m, Height = 70m, Notes = "ER admission - STEMI" },
            new Vitals { Id = 15, PatientId = 15, RecordedAt = today.AddDays(-44).AddHours(10), Temperature = 98.4m, HeartRate = 78, SystolicBp = 132, DiastolicBp = 82, RespiratoryRate = 18, OxygenSaturation = 97m, Weight = 200m, Height = 70m, Notes = "Post-PCI day 1 - stable" },
            new Vitals { Id = 16, PatientId = 6, RecordedAt = today.AddDays(-21).AddHours(10), Temperature = 98.6m, HeartRate = 70, SystolicBp = 110, DiastolicBp = 68, RespiratoryRate = 16, OxygenSaturation = 99m, Weight = 125m, Height = 63m },
            new Vitals { Id = 17, PatientId = 13, RecordedAt = today.AddDays(-30).AddHours(11), Temperature = 98.4m, HeartRate = 65, SystolicBp = 108, DiastolicBp = 66, RespiratoryRate = 16, OxygenSaturation = 99m, Weight = 145m, Height = 67m, Notes = "Healthy adolescent" },
            // Additional historical vitals for trending
            new Vitals { Id = 18, PatientId = 1, RecordedAt = today.AddDays(-180).AddHours(10), Temperature = 98.4m, HeartRate = 70, SystolicBp = 120, DiastolicBp = 76, RespiratoryRate = 16, OxygenSaturation = 99m, Weight = 172m, Height = 70m },
            new Vitals { Id = 19, PatientId = 3, RecordedAt = today.AddDays(-90).AddHours(9), Temperature = 98.6m, HeartRate = 78, SystolicBp = 130, DiastolicBp = 82, RespiratoryRate = 16, OxygenSaturation = 98m, Weight = 192m, Height = 68m },
            new Vitals { Id = 20, PatientId = 5, RecordedAt = today.AddDays(-90).AddHours(10), Temperature = 98.4m, HeartRate = 82, SystolicBp = 162, DiastolicBp = 98, RespiratoryRate = 18, OxygenSaturation = 97m, Weight = 218m, Height = 72m, Notes = "Pre-treatment baseline" },
            new Vitals { Id = 21, PatientId = 2, RecordedAt = today.AddDays(-60).AddHours(9), Temperature = 98.6m, HeartRate = 86, SystolicBp = 120, DiastolicBp = 76, RespiratoryRate = 16, OxygenSaturation = 98m, Weight = 136m, Height = 64m },
            new Vitals { Id = 22, PatientId = 7, RecordedAt = today.AddDays(-90).AddHours(14), Temperature = 98.4m, HeartRate = 72, SystolicBp = 128, DiastolicBp = 78, RespiratoryRate = 16, OxygenSaturation = 98m, Weight = 186m, Height = 71m },
            new Vitals { Id = 23, PatientId = 10, RecordedAt = today.AddDays(-90).AddHours(10), Temperature = 98.2m, HeartRate = 66, SystolicBp = 138, DiastolicBp = 86, RespiratoryRate = 16, OxygenSaturation = 97m, Weight = 157m, Height = 63m },
            new Vitals { Id = 24, PatientId = 15, RecordedAt = today.AddDays(-30).AddHours(10), Temperature = 98.4m, HeartRate = 72, SystolicBp = 128, DiastolicBp = 78, RespiratoryRate = 16, OxygenSaturation = 97m, Weight = 198m, Height = 70m, Notes = "Post-discharge follow-up" },
            new Vitals { Id = 25, PatientId = 9, RecordedAt = today.AddDays(-180).AddHours(9), Temperature = 98.6m, HeartRate = 70, SystolicBp = 118, DiastolicBp = 74, RespiratoryRate = 16, OxygenSaturation = 99m, Weight = 168m, Height = 69m }
        );
    }
}
