using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EhrApi.Migrations
{
    /// <inheritdoc />
    public partial class ExpandedSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InsuranceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EmergencyContactName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BloodType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Specialty = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LicenseNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Department = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Allergies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    Allergen = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AllergyType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Reaction = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Allergies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Allergies_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    OrderedByProviderId = table.Column<int>(type: "integer", nullable: true),
                    TestName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TestCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Result = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReferenceRange = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Flag = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OrderedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabResults_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vitals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Temperature = table.Column<decimal>(type: "numeric", nullable: true),
                    HeartRate = table.Column<int>(type: "integer", nullable: true),
                    SystolicBp = table.Column<int>(type: "integer", nullable: true),
                    DiastolicBp = table.Column<int>(type: "integer", nullable: true),
                    RespiratoryRate = table.Column<int>(type: "integer", nullable: true),
                    OxygenSaturation = table.Column<decimal>(type: "numeric", nullable: true),
                    Weight = table.Column<decimal>(type: "numeric", nullable: true),
                    Height = table.Column<decimal>(type: "numeric", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vitals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vitals_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    ProviderId = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Appointments_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicalRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    ProviderId = table.Column<int>(type: "integer", nullable: false),
                    EncounterDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EncounterType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChiefComplaint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Diagnosis = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DiagnosisCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TreatmentPlan = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ClinicalNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FollowUpInstructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalRecords_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicalRecords_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Prescriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    ProviderId = table.Column<int>(type: "integer", nullable: false),
                    MedicationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Dosage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Frequency = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Route = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Refills = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Instructions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Pharmacy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Patients",
                columns: new[] { "Id", "Address", "BloodType", "CreatedAt", "DateOfBirth", "Email", "EmergencyContactName", "EmergencyContactPhone", "FirstName", "Gender", "InsuranceId", "LastName", "Phone", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "123 Main St, Toronto, ON", "O+", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1985, 3, 15, 0, 0, 0, 0, DateTimeKind.Utc), "john.doe@email.com", "Jane Doe", "416-555-1002", "John", "Male", "INS-001", "Doe", "416-555-1001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "456 Oak Ave, Toronto, ON", "A+", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1992, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc), "emily.smith@email.com", "Bob Smith", "416-555-1004", "Emily", "Female", "INS-002", "Smith", "416-555-1003", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "789 Pine Rd, Mississauga, ON", "B-", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1978, 11, 8, 0, 0, 0, 0, DateTimeKind.Utc), "m.johnson@email.com", null, null, "Michael", "Male", "INS-003", "Johnson", "416-555-1005", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "22 Queen St W, Toronto, ON", "AB+", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2001, 5, 12, 0, 0, 0, 0, DateTimeKind.Utc), "sophia.b@email.com", "Linda Brown", "416-555-1007", "Sophia", "Female", "INS-004", "Brown", "416-555-1006", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, "50 Lakeshore Blvd, Oakville, ON", "A-", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1965, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), "w.taylor@email.com", "Patricia Taylor", "905-555-2002", "William", "Male", "INS-005", "Taylor", "905-555-2001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, "88 Dundas St E, Toronto, ON", "O-", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1998, 12, 28, 0, 0, 0, 0, DateTimeKind.Utc), "olivia.m@email.com", null, null, "Olivia", "Female", "INS-006", "Martinez", "647-555-3001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, "15 Yonge St, Toronto, ON", "B+", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1970, 1, 19, 0, 0, 0, 0, DateTimeKind.Utc), "liam.a@email.com", "Carol Anderson", "416-555-4002", "Liam", "Male", "INS-007", "Anderson", "416-555-4001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, "200 Bloor St W, Toronto, ON", "A+", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2010, 8, 5, 0, 0, 0, 0, DateTimeKind.Utc), "ava.parent@email.com", "Mark Thomas", "416-555-5002", "Ava", "Female", "INS-008", "Thomas", "416-555-5001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, "33 King St, Hamilton, ON", "O+", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1990, 4, 17, 0, 0, 0, 0, DateTimeKind.Utc), "noah.j@email.com", null, null, "Noah", "Male", "INS-009", "Jackson", "905-555-6001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 10, "77 College St, Toronto, ON", "AB-", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1955, 2, 14, 0, 0, 0, 0, DateTimeKind.Utc), "isabella.w@email.com", "George White", "416-555-7002", "Isabella", "Female", "INS-010", "White", "416-555-7001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 11, "5 Eglinton Ave, Toronto, ON", "A+", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1988, 6, 30, 0, 0, 0, 0, DateTimeKind.Utc), "ethan.h@email.com", null, null, "Ethan", "Male", "INS-011", "Harris", "647-555-8001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 12, "140 Bay St, Toronto, ON", "O+", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1975, 10, 22, 0, 0, 0, 0, DateTimeKind.Utc), "mia.c@email.com", "David Clark", "416-555-9002", "Mia", "Female", "INS-012", "Clark", "416-555-9001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 13, "60 Hurontario St, Brampton, ON", "B+", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2005, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), "alex.l.parent@email.com", "Susan Lewis", "905-555-1102", "Alexander", "Male", "INS-013", "Lewis", "905-555-1101", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 14, "95 Spadina Ave, Toronto, ON", "A-", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1982, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), "charlotte.w@email.com", null, null, "Charlotte", "Female", "INS-014", "Walker", "416-555-1201", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 15, "180 Dundas St W, London, ON", "O-", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1960, 11, 25, 0, 0, 0, 0, DateTimeKind.Utc), "ben.hall@email.com", "Margaret Hall", "905-555-1302", "Benjamin", "Male", "INS-015", "Hall", "905-555-1301", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Providers",
                columns: new[] { "Id", "CreatedAt", "Department", "Email", "FirstName", "IsActive", "LastName", "LicenseNumber", "Phone", "Specialty" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Primary Care", "s.chen@ehrhealth.ca", "Sarah", true, "Chen", "FM-2024-001", "416-555-0101", "Family Medicine" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cardiology", "j.wilson@ehrhealth.ca", "James", true, "Wilson", "CD-2024-002", "416-555-0102", "Cardiology" },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pediatrics", "m.garcia@ehrhealth.ca", "Maria", true, "Garcia", "PD-2024-003", "416-555-0103", "Pediatrics" },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Orthopedics", "d.kim@ehrhealth.ca", "David", true, "Kim", "OR-2024-004", "416-555-0104", "Orthopedics" },
                    { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dermatology", "l.patel@ehrhealth.ca", "Lisa", true, "Patel", "DM-2024-005", "416-555-0105", "Dermatology" },
                    { 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Neurology", "r.thompson@ehrhealth.ca", "Robert", true, "Thompson", "NR-2024-006", "416-555-0106", "Neurology" },
                    { 7, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Oncology", "a.russo@ehrhealth.ca", "Angela", true, "Russo", "ON-2024-007", "416-555-0107", "Oncology" },
                    { 8, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Emergency", "k.nguyen@ehrhealth.ca", "Kevin", true, "Nguyen", "EM-2024-008", "416-555-0108", "Emergency Medicine" }
                });

            migrationBuilder.InsertData(
                table: "Allergies",
                columns: new[] { "Id", "Allergen", "AllergyType", "IsActive", "PatientId", "Reaction", "ReportedAt", "Severity" },
                values: new object[,]
                {
                    { 1, "Penicillin", "Drug", true, 1, "Anaphylaxis", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Severe" },
                    { 2, "Peanuts", "Food", true, 2, "Anaphylaxis, swelling", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Life-Threatening" },
                    { 3, "Dust Mites", "Environmental", true, 1, "Sneezing, congestion", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mild" },
                    { 4, "Sulfa Drugs", "Drug", true, 5, "Skin rash, hives", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Moderate" },
                    { 5, "Codeine", "Drug", true, 7, "Respiratory depression, nausea", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Severe" },
                    { 6, "Latex", "Environmental", true, 10, "Contact dermatitis, itching", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Moderate" },
                    { 7, "Shellfish", "Food", true, 6, "Hives, throat swelling", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Severe" },
                    { 8, "Amoxicillin", "Drug", true, 8, "Rash, GI upset", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Moderate" },
                    { 9, "Iodine Contrast Dye", "Drug", true, 12, "Anaphylaxis", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Severe" },
                    { 10, "Aspirin", "Drug", false, 15, "GI upset - tolerates low dose", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mild" },
                    { 11, "Pollen", "Environmental", true, 9, "Seasonal rhinitis", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mild" },
                    { 12, "Dairy", "Food", true, 4, "Bloating, GI discomfort", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mild" }
                });

            migrationBuilder.InsertData(
                table: "Appointments",
                columns: new[] { "Id", "CreatedAt", "DurationMinutes", "Notes", "PatientId", "ProviderId", "Reason", "ScheduledAt", "Status", "Type", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Patient appears healthy overall", 1, 1, "Annual physical exam", new DateTime(2026, 3, 22, 9, 0, 0, 0, DateTimeKind.Utc), "Completed", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 45, "Results reviewed with patient", 2, 2, "Cardiac follow-up after echocardiogram", new DateTime(2026, 3, 22, 9, 30, 0, 0, DateTimeKind.Utc), "Completed", "FollowUp", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, null, 5, 1, "Blood pressure management review", new DateTime(2026, 3, 22, 10, 0, 0, 0, DateTimeKind.Utc), "InProgress", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, null, 8, 3, "Growth checkup - pediatric visit", new DateTime(2026, 3, 22, 10, 30, 0, 0, DateTimeKind.Utc), "CheckedIn", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 45, null, 11, 4, "Knee pain evaluation", new DateTime(2026, 3, 22, 11, 0, 0, 0, DateTimeKind.Utc), "Scheduled", "Specialist", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, null, 6, 5, "Skin rash follow-up", new DateTime(2026, 3, 22, 13, 0, 0, 0, DateTimeKind.Utc), "Scheduled", "Specialist", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 60, null, 10, 6, "Neurological assessment - headaches", new DateTime(2026, 3, 22, 14, 0, 0, 0, DateTimeKind.Utc), "Scheduled", "Specialist", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, null, 15, 2, "Post-stent implant follow-up", new DateTime(2026, 3, 22, 15, 0, 0, 0, DateTimeKind.Utc), "Scheduled", "FollowUp", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 45, "ECG normal, stress test ordered", 1, 2, "Chest pain evaluation", new DateTime(2026, 3, 8, 10, 0, 0, 0, DateTimeKind.Utc), "Completed", "Specialist", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 10, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "HbA1c slightly elevated, adjusted metformin", 3, 1, "Diabetes management", new DateTime(2026, 3, 12, 9, 0, 0, 0, DateTimeKind.Utc), "Completed", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 11, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Prescribed topical retinoid", 4, 5, "Acne treatment", new DateTime(2026, 3, 15, 11, 0, 0, 0, DateTimeKind.Utc), "Completed", "Specialist", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 12, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 60, "MRI ordered, started preventive medication", 7, 6, "Migraine evaluation", new DateTime(2026, 3, 17, 14, 0, 0, 0, DateTimeKind.Utc), "Completed", "Specialist", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 13, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Viral infection, rest and fluids recommended", 9, 1, "Flu symptoms", new DateTime(2026, 3, 19, 9, 30, 0, 0, DateTimeKind.Utc), "Completed", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 14, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 60, "Mammogram results normal", 12, 7, "Breast cancer screening follow-up", new DateTime(2026, 3, 20, 10, 0, 0, 0, DateTimeKind.Utc), "Completed", "Specialist", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 15, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Referred to psychiatrist, sleep hygiene discussed", 14, 1, "Anxiety and insomnia", new DateTime(2026, 3, 21, 15, 0, 0, 0, DateTimeKind.Utc), "Completed", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 16, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, "Patient cancelled due to scheduling conflict", 6, 1, "Sore throat", new DateTime(2026, 3, 18, 10, 0, 0, 0, DateTimeKind.Utc), "Cancelled", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 17, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, null, 13, 3, "Sports physical", new DateTime(2026, 3, 16, 11, 0, 0, 0, DateTimeKind.Utc), "NoShow", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 18, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 45, null, 3, 2, "Cardiac stress test results review", new DateTime(2026, 3, 25, 10, 0, 0, 0, DateTimeKind.Utc), "Scheduled", "FollowUp", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 19, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 60, null, 7, 6, "MRI results review - migraines", new DateTime(2026, 3, 29, 14, 0, 0, 0, DateTimeKind.Utc), "Scheduled", "FollowUp", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 20, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 45, null, 9, 4, "Lower back pain evaluation", new DateTime(2026, 4, 1, 11, 0, 0, 0, DateTimeKind.Utc), "Scheduled", "Specialist", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "LabResults",
                columns: new[] { "Id", "CompletedAt", "Flag", "Notes", "OrderedAt", "OrderedByProviderId", "PatientId", "ReferenceRange", "Result", "Status", "TestCode", "TestName", "Unit" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 3, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Normal", "All values within normal limits", new DateTime(2026, 3, 21, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1, "WBC 4.5-11.0", "WBC 7.2, RBC 4.8, Hgb 14.5, Plt 250", "Completed", "58410-2", "Complete Blood Count (CBC)", "various" },
                    { 2, new DateTime(2026, 3, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Normal", null, new DateTime(2026, 3, 21, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1, "Glucose 70-100", "Glucose 95, BUN 18, Creatinine 1.0, Na 140, K 4.2", "Completed", "24323-8", "Comprehensive Metabolic Panel", "various" },
                    { 3, new DateTime(2026, 3, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Normal", null, new DateTime(2026, 3, 21, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1, "Total <200", "Total Chol 198, LDL 120, HDL 55, Triglycerides 115", "Completed", "24331-1", "Lipid Panel", "mg/dL" },
                    { 4, new DateTime(2026, 3, 12, 0, 0, 0, 0, DateTimeKind.Utc), "High", "Elevated - indicates suboptimal diabetes control", new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Utc), 1, 3, "<5.7 normal, 5.7-6.4 prediabetes", "7.8", "Completed", "4548-4", "Hemoglobin A1c", "%" },
                    { 5, new DateTime(2026, 3, 12, 0, 0, 0, 0, DateTimeKind.Utc), "High", null, new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Utc), 1, 3, "70-100 mg/dL", "145", "Completed", "1558-6", "Fasting Blood Glucose", "mg/dL" },
                    { 6, new DateTime(2026, 2, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Normal", null, new DateTime(2026, 2, 18, 0, 0, 0, 0, DateTimeKind.Utc), 2, 5, "Creatinine 0.7-1.3", "Glucose 102, BUN 22, Creatinine 1.1, Na 141, K 4.5", "Completed", "51990-0", "Basic Metabolic Panel", "various" },
                    { 7, new DateTime(2026, 2, 20, 0, 0, 0, 0, DateTimeKind.Utc), "High", "Elevated LDL and triglycerides. Statin therapy initiated.", new DateTime(2026, 2, 18, 0, 0, 0, 0, DateTimeKind.Utc), 2, 5, "LDL <100 optimal", "Total Chol 245, LDL 165, HDL 42, Triglycerides 190", "Completed", "24331-1", "Lipid Panel", "mg/dL" },
                    { 8, new DateTime(2026, 3, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Normal", null, new DateTime(2026, 3, 17, 0, 0, 0, 0, DateTimeKind.Utc), 2, 2, "0.4-4.0 mIU/L", "2.5", "Completed", "3016-3", "Thyroid Stimulating Hormone (TSH)", "mIU/L" },
                    { 9, new DateTime(2026, 3, 17, 0, 0, 0, 0, DateTimeKind.Utc), "Normal", "Ruled out acute MI", new DateTime(2026, 3, 17, 0, 0, 0, 0, DateTimeKind.Utc), 2, 2, "<0.04 ng/mL", "0.02", "Completed", "10839-9", "Troponin I", "ng/mL" },
                    { 10, new DateTime(2026, 2, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Critical", "Markedly elevated - consistent with acute MI", new DateTime(2026, 2, 5, 0, 0, 0, 0, DateTimeKind.Utc), 2, 15, "<0.04 ng/mL", "2.5", "Completed", "10839-9", "Troponin I", "ng/mL" },
                    { 11, new DateTime(2026, 2, 5, 0, 0, 0, 0, DateTimeKind.Utc), "High", "Mild leukocytosis, likely stress response", new DateTime(2026, 2, 5, 0, 0, 0, 0, DateTimeKind.Utc), 2, 15, "WBC 4.5-11.0", "WBC 11.2, RBC 4.1, Hgb 12.8, Plt 310", "Completed", "58410-2", "Complete Blood Count (CBC)", "various" },
                    { 12, new DateTime(2026, 3, 2, 0, 0, 0, 0, DateTimeKind.Utc), "Low", "Low B12 may contribute to cognitive symptoms", new DateTime(2026, 2, 28, 0, 0, 0, 0, DateTimeKind.Utc), 6, 10, "200-900 pg/mL", "180", "Completed", "2132-9", "Vitamin B12", "pg/mL" },
                    { 13, new DateTime(2026, 3, 2, 0, 0, 0, 0, DateTimeKind.Utc), "Normal", null, new DateTime(2026, 2, 28, 0, 0, 0, 0, DateTimeKind.Utc), 6, 10, "0.4-4.0 mIU/L", "3.8", "Completed", "3016-3", "Thyroid Stimulating Hormone (TSH)", "mIU/L" },
                    { 14, new DateTime(2026, 3, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Normal", null, new DateTime(2026, 3, 19, 0, 0, 0, 0, DateTimeKind.Utc), 1, 9, "Negative", "Negative", "Completed", "6558-9", "Rapid Strep Test", "Qualitative" },
                    { 15, new DateTime(2026, 3, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Normal", null, new DateTime(2026, 3, 17, 0, 0, 0, 0, DateTimeKind.Utc), 7, 12, "<35 U/mL", "18", "Completed", "10334-1", "CA-125", "U/mL" },
                    { 16, new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Normal", null, new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Utc), 4, 11, "<3.0 mg/L", "0.8", "Completed", "1988-5", "C-Reactive Protein (CRP)", "mg/L" },
                    { 17, null, null, "Ordered for chronic migraine evaluation", new DateTime(2026, 3, 17, 0, 0, 0, 0, DateTimeKind.Utc), 6, 7, null, null, "Pending", "30799-1", "MRI Brain", null },
                    { 18, null, null, "Ordered for cognitive impairment workup", new DateTime(2026, 3, 19, 0, 0, 0, 0, DateTimeKind.Utc), 6, 10, null, null, "Pending", "30799-1", "MRI Brain with Contrast", null },
                    { 19, null, null, "Rule out thyroid contribution to anxiety/insomnia", new DateTime(2026, 3, 22, 0, 0, 0, 0, DateTimeKind.Utc), 1, 14, null, null, "Pending", "24348-5", "Thyroid Panel", null },
                    { 20, null, null, "Baseline renal function on ACE inhibitor therapy", new DateTime(2026, 3, 22, 0, 0, 0, 0, DateTimeKind.Utc), 2, 5, null, null, "Pending", "24362-6", "Renal Function Panel", null }
                });

            migrationBuilder.InsertData(
                table: "MedicalRecords",
                columns: new[] { "Id", "ChiefComplaint", "ClinicalNotes", "CreatedAt", "Diagnosis", "DiagnosisCode", "EncounterDate", "EncounterType", "FollowUpInstructions", "PatientId", "ProviderId", "TreatmentPlan", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Annual physical exam", "Patient is in good health. BMI 24.5. All vitals within normal limits.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Healthy adult, mild seasonal allergies", "Z00.00", new DateTime(2026, 3, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Office Visit", "Annual follow-up in 1 year", 1, 1, "Continue current allergy medication. Return in 12 months.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "Follow-up for heart palpitations", "Echo results normal. EF 60%. No structural abnormalities.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Benign premature ventricular contractions", "I49.3", new DateTime(2026, 3, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Office Visit", "Return in 2 weeks with Holter monitor results", 2, 2, "Monitor with Holter for 48 hours. Reduce caffeine intake.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "Diabetes management", "HbA1c 7.8%. Fasting glucose 145 mg/dL. Peripheral neuropathy screening negative.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Type 2 Diabetes Mellitus, uncontrolled", "E11.65", new DateTime(2026, 3, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Office Visit", "Repeat HbA1c in 3 months. Follow up in 4 weeks.", 3, 1, "Increase Metformin to 1000mg BID. Dietary counseling referral.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "Hypertension management", "BP 158/95 mmHg. BMI 28.3. Kidney function tests normal. Started DASH diet counseling.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Essential hypertension, Stage 2", "I10", new DateTime(2026, 2, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Office Visit", "BP check in 2 weeks. Full follow-up in 4 weeks.", 5, 2, "Started Lisinopril 10mg daily. Low sodium diet recommended.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, "Persistent acne", "Moderate inflammatory acne on face and upper back. No scarring noted.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Acne vulgaris, moderate", "L70.0", new DateTime(2026, 3, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Office Visit", "Follow up in 6 weeks to assess response", 4, 5, "Topical tretinoin 0.025% nightly. Benzoyl peroxide 5% wash AM.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, "Recurrent migraines", "Patient reports 4-5 migraines per month, lasting 6-12 hours. Photophobia and nausea present. Neurological exam normal.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Migraine without aura, chronic", "G43.709", new DateTime(2026, 3, 17, 0, 0, 0, 0, DateTimeKind.Utc), "Office Visit", "MRI brain within 2 weeks. Follow up after results.", 7, 6, "Sumatriptan 50mg PRN for acute attacks. Topiramate 25mg daily for prevention. MRI brain ordered.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, "Well-child visit", "Growth tracking: 75th percentile height, 60th percentile weight. Development milestones on track.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Routine child health examination", "Z00.129", new DateTime(2026, 1, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Office Visit", "Next well-child visit in 6 months", 8, 3, "Vaccines up to date. Next visit in 6 months.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, "Fever, cough, body aches for 3 days", "Temp 100.8F. Mild pharyngeal erythema. Lungs clear bilaterally. Rapid strep negative.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Acute upper respiratory infection", "J06.9", new DateTime(2026, 3, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Office Visit", "Return if symptoms worsen or persist beyond 7 days", 9, 1, "Symptomatic treatment. Acetaminophen 500mg q6h PRN. Rest, fluids.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, "Memory concerns and headaches", "MMSE score 24/30. Short-term memory deficits noted. Cranial nerves intact. No focal deficits.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mild cognitive impairment", "G31.84", new DateTime(2026, 3, 2, 0, 0, 0, 0, DateTimeKind.Utc), "Office Visit", "Complete MRI and neuropsych testing. Follow up in 4 weeks.", 10, 6, "Neuropsychological testing ordered. Donepezil 5mg daily. Brain MRI scheduled.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 10, "Breast cancer screening follow-up", "Bilateral mammogram BI-RADS 1. No suspicious findings. Patient reassured.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Screening mammogram - normal", "Z12.31", new DateTime(2026, 3, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Office Visit", "Annual mammogram in 12 months", 12, 7, "Continue annual mammography. No further intervention needed.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 11, "Left knee pain for 3 weeks", "Tenderness over patellar tendon. Full ROM with pain at terminal extension. No effusion. X-ray unremarkable.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Patellar tendinitis", "M76.50", new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Office Visit", "Follow up in 6 weeks. Sooner if worsening.", 11, 4, "Physical therapy 2x/week for 6 weeks. Ibuprofen 400mg TID with food. Ice therapy.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 12, "Anxiety and difficulty sleeping", "Patient reports persistent worry, difficulty falling asleep for past 2 months. PHQ-9 score 8. GAD-7 score 12.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Generalized anxiety disorder with insomnia", "F41.1", new DateTime(2026, 3, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Telehealth", "Psychiatric evaluation within 2 weeks. Follow up in 4 weeks.", 14, 1, "Sleep hygiene education. Melatonin 3mg at bedtime. Psychiatric referral for CBT.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 13, "Acute chest pain radiating to left arm", "Troponin elevated at 2.5 ng/mL. ECG showed ST elevation in leads II, III, aVF. Cath lab activated. Successful PCI to RCA.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Acute myocardial infarction - STEMI", "I21.0", new DateTime(2026, 2, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Hospitalization", "Cardiac rehab 3x/week. Follow up in 2 weeks. Take all medications as prescribed.", 15, 2, "Emergency PCI with drug-eluting stent. Dual antiplatelet therapy. Cardiac rehabilitation.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 14, "Itchy rash on arms and legs", "Erythematous papular rash on bilateral forearms and lower legs. No vesicles. Likely contact irritant.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Contact dermatitis", "L25.9", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Office Visit", "Follow up in 2 weeks if not improving", 6, 5, "Triamcinolone cream 0.1% BID to affected areas. Avoid known irritants.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 15, "Pre-sports physical", "No murmurs, no musculoskeletal issues. Normal vision screening. BMI appropriate for age.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Healthy adolescent", "Z02.5", new DateTime(2026, 2, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Office Visit", "Annual physical in 1 year", 13, 3, "Cleared for sports participation. No restrictions.", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Prescriptions",
                columns: new[] { "Id", "CreatedAt", "Dosage", "EndDate", "Frequency", "Instructions", "MedicationName", "PatientId", "Pharmacy", "ProviderId", "Refills", "Route", "StartDate", "Status" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "10mg", null, "Once daily", "Take once daily for allergies. May cause drowsiness.", "Cetirizine (Zyrtec)", 1, "Shoppers Drug Mart - 123 Main St", 1, 5, "Oral", new DateTime(2025, 12, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "1000mg", null, "Twice daily", "Take with meals to reduce GI side effects. Monitor blood sugar regularly.", "Metformin", 3, "Rexall Pharmacy - 789 Pine Rd", 1, 3, "Oral", new DateTime(2026, 3, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "10mg", null, "Once daily", "Take in the morning. Monitor blood pressure. Report persistent cough.", "Lisinopril", 5, "Costco Pharmacy - Oakville", 2, 5, "Oral", new DateTime(2026, 2, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "0.025%", null, "Once daily at bedtime", "Apply thin layer to clean, dry skin at night. Use sunscreen during the day.", "Tretinoin Cream", 4, "Shoppers Drug Mart - Queen St", 5, 2, "Topical", new DateTime(2026, 3, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "50mg", null, "As needed for migraine", "Take at onset of migraine. May repeat after 2 hours. Max 200mg/day.", "Sumatriptan", 7, "Pharmasave - 15 Yonge St", 6, 3, "Oral", new DateTime(2026, 3, 17, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "25mg", null, "Once daily", "Take at bedtime. Increase to 50mg after 2 weeks if tolerated.", "Topiramate", 7, "Pharmasave - 15 Yonge St", 6, 5, "Oral", new DateTime(2026, 3, 17, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 7, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "5mg", null, "Once daily", "Take at bedtime. May increase to 10mg after 4-6 weeks.", "Donepezil (Aricept)", 10, "Shoppers Drug Mart - College St", 6, 5, "Oral", new DateTime(2026, 3, 2, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 8, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "400mg", new DateTime(2026, 4, 18, 0, 0, 0, 0, DateTimeKind.Utc), "Three times daily", "Take with food. Discontinue if stomach upset occurs.", "Ibuprofen", 11, "Rexall Pharmacy - Eglinton Ave", 4, 0, "Oral", new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 9, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "81mg", null, "Once daily", "Take daily. Do not stop without consulting doctor.", "Aspirin", 15, "London Drugs - Dundas St", 2, 11, "Oral", new DateTime(2026, 2, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 10, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "75mg", new DateTime(2027, 2, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Once daily", "Take daily for 12 months post-stent. Do not stop without consulting cardiologist.", "Clopidogrel (Plavix)", 15, "London Drugs - Dundas St", 2, 11, "Oral", new DateTime(2026, 2, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 11, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "80mg", null, "Once daily at bedtime", "Take at bedtime. Report muscle pain or weakness.", "Atorvastatin (Lipitor)", 15, "London Drugs - Dundas St", 2, 11, "Oral", new DateTime(2026, 2, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 12, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "0.1%", new DateTime(2026, 3, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Twice daily", "Apply thin layer to rash areas. Do not use on face.", "Triamcinolone Cream", 6, "Shoppers Drug Mart - Dundas St", 5, 0, "Topical", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Completed" },
                    { 13, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "3mg", null, "Once daily at bedtime", "Take 30 minutes before bedtime. Maintain regular sleep schedule.", "Melatonin", 14, "Shoppers Drug Mart - Spadina Ave", 1, 2, "Oral", new DateTime(2026, 3, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 14, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "500mg", new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Every 6 hours as needed", "Take as needed for fever/pain. Max 4000mg/day.", "Acetaminophen (Tylenol)", 9, "Rexall Pharmacy - King St", 1, 0, "Oral", new DateTime(2026, 3, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 15, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "25mg", null, "Twice daily", "Take with meals. Do not stop abruptly.", "Metoprolol", 2, "Shoppers Drug Mart - Oak Ave", 2, 5, "Oral", new DateTime(2026, 1, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 16, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "20mg", null, "Once daily at bedtime", "Take at bedtime. Annual lipid panel required.", "Atorvastatin (Lipitor)", 5, "Costco Pharmacy - Oakville", 1, 5, "Oral", new DateTime(2025, 12, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 17, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "5mg", null, "Once daily before breakfast", "Take 30 minutes before breakfast. Monitor for low blood sugar.", "Glipizide", 3, "Rexall Pharmacy - 789 Pine Rd", 1, 5, "Oral", new DateTime(2025, 9, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Active" },
                    { 18, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "81mg", new DateTime(2026, 2, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Once daily", "Low-dose aspirin for cardiac prevention. Discontinued per provider review.", "Aspirin", 1, "Shoppers Drug Mart - 123 Main St", 2, 0, "Oral", new DateTime(2025, 9, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Completed" }
                });

            migrationBuilder.InsertData(
                table: "Vitals",
                columns: new[] { "Id", "DiastolicBp", "HeartRate", "Height", "Notes", "OxygenSaturation", "PatientId", "RecordedAt", "RespiratoryRate", "SystolicBp", "Temperature", "Weight" },
                values: new object[,]
                {
                    { 1, 78, 72, 70m, "Annual physical", 99m, 1, new DateTime(2026, 3, 22, 9, 0, 0, 0, DateTimeKind.Utc), 16, 122, 98.6m, 175m },
                    { 2, 74, 82, 64m, null, 98m, 2, new DateTime(2026, 3, 22, 9, 30, 0, 0, DateTimeKind.Utc), 18, 118, 98.4m, 135m },
                    { 3, 92, 78, 72m, "BP still elevated despite medication", 97m, 5, new DateTime(2026, 3, 22, 10, 0, 0, 0, DateTimeKind.Utc), 16, 148, 98.2m, 210m },
                    { 4, 65, 88, 54m, "Pediatric patient", 99m, 8, new DateTime(2026, 3, 22, 10, 30, 0, 0, DateTimeKind.Utc), 20, 100, 98.8m, 72m },
                    { 5, 84, 76, 68m, null, 98m, 3, new DateTime(2026, 3, 12, 9, 0, 0, 0, DateTimeKind.Utc), 16, 134, 98.4m, 195m },
                    { 6, 95, 80, 72m, "Initial visit - BP elevated", 97m, 5, new DateTime(2026, 2, 20, 10, 0, 0, 0, DateTimeKind.Utc), 18, 158, 98.6m, 215m },
                    { 7, 70, 70, 65m, null, 99m, 4, new DateTime(2026, 3, 15, 11, 0, 0, 0, DateTimeKind.Utc), 16, 112, 98.4m, 130m },
                    { 8, 80, 74, 71m, null, 98m, 7, new DateTime(2026, 3, 17, 14, 0, 0, 0, DateTimeKind.Utc), 16, 126, 98.6m, 185m },
                    { 9, 76, 92, 69m, "Febrile - URI", 97m, 9, new DateTime(2026, 3, 19, 9, 30, 0, 0, DateTimeKind.Utc), 20, 120, 100.8m, 170m },
                    { 10, 88, 68, 63m, null, 96m, 10, new DateTime(2026, 3, 2, 14, 0, 0, 0, DateTimeKind.Utc), 16, 142, 97.8m, 155m },
                    { 11, 82, 72, 66m, null, 98m, 12, new DateTime(2026, 3, 20, 10, 0, 0, 0, DateTimeKind.Utc), 16, 128, 98.4m, 160m },
                    { 12, 72, 68, 73m, null, 99m, 11, new DateTime(2026, 3, 7, 11, 0, 0, 0, DateTimeKind.Utc), 14, 118, 98.6m, 180m },
                    { 13, 78, 84, 65m, "Mildly tachycardic - anxious", 99m, 14, new DateTime(2026, 3, 21, 15, 0, 0, 0, DateTimeKind.Utc), 18, 124, 98.2m, 140m },
                    { 14, 100, 105, 70m, "ER admission - STEMI", 94m, 15, new DateTime(2026, 2, 5, 8, 0, 0, 0, DateTimeKind.Utc), 22, 165, 98.8m, 200m },
                    { 15, 82, 78, 70m, "Post-PCI day 1 - stable", 97m, 15, new DateTime(2026, 2, 6, 10, 0, 0, 0, DateTimeKind.Utc), 18, 132, 98.4m, 200m },
                    { 16, 68, 70, 63m, null, 99m, 6, new DateTime(2026, 3, 1, 10, 0, 0, 0, DateTimeKind.Utc), 16, 110, 98.6m, 125m },
                    { 17, 66, 65, 67m, "Healthy adolescent", 99m, 13, new DateTime(2026, 2, 20, 11, 0, 0, 0, DateTimeKind.Utc), 16, 108, 98.4m, 145m },
                    { 18, 76, 70, 70m, null, 99m, 1, new DateTime(2025, 9, 23, 10, 0, 0, 0, DateTimeKind.Utc), 16, 120, 98.4m, 172m },
                    { 19, 82, 78, 68m, null, 98m, 3, new DateTime(2025, 12, 22, 9, 0, 0, 0, DateTimeKind.Utc), 16, 130, 98.6m, 192m },
                    { 20, 98, 82, 72m, "Pre-treatment baseline", 97m, 5, new DateTime(2025, 12, 22, 10, 0, 0, 0, DateTimeKind.Utc), 18, 162, 98.4m, 218m },
                    { 21, 76, 86, 64m, null, 98m, 2, new DateTime(2026, 1, 21, 9, 0, 0, 0, DateTimeKind.Utc), 16, 120, 98.6m, 136m },
                    { 22, 78, 72, 71m, null, 98m, 7, new DateTime(2025, 12, 22, 14, 0, 0, 0, DateTimeKind.Utc), 16, 128, 98.4m, 186m },
                    { 23, 86, 66, 63m, null, 97m, 10, new DateTime(2025, 12, 22, 10, 0, 0, 0, DateTimeKind.Utc), 16, 138, 98.2m, 157m },
                    { 24, 78, 72, 70m, "Post-discharge follow-up", 97m, 15, new DateTime(2026, 2, 20, 10, 0, 0, 0, DateTimeKind.Utc), 16, 128, 98.4m, 198m },
                    { 25, 74, 70, 69m, null, 99m, 9, new DateTime(2025, 9, 23, 9, 0, 0, 0, DateTimeKind.Utc), 16, 118, 98.6m, 168m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Allergies_PatientId",
                table: "Allergies",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId",
                table: "Appointments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ProviderId",
                table: "Appointments",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ScheduledAt",
                table: "Appointments",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_PatientId",
                table: "LabResults",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_PatientId",
                table: "MedicalRecords",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_ProviderId",
                table: "MedicalRecords",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_InsuranceId",
                table: "Patients",
                column: "InsuranceId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_LastName_FirstName",
                table: "Patients",
                columns: new[] { "LastName", "FirstName" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PatientId",
                table: "Prescriptions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_ProviderId",
                table: "Prescriptions",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Vitals_PatientId",
                table: "Vitals",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Allergies");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "LabResults");

            migrationBuilder.DropTable(
                name: "MedicalRecords");

            migrationBuilder.DropTable(
                name: "Prescriptions");

            migrationBuilder.DropTable(
                name: "Vitals");

            migrationBuilder.DropTable(
                name: "Providers");

            migrationBuilder.DropTable(
                name: "Patients");
        }
    }
}
