using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Interfaces;

public interface IMedicalRecordService
{
    // Vaccination (MED-01)
    Task<Result<VaccinationRecordDto>> CreateVaccinationAsync(CreateVaccinationDto dto, string vetUserId);
    Task<VaccinationRecordDto?> GetVaccinationByIdAsync(int id);
    Task<IEnumerable<VaccinationRecordDto>> GetVaccinationsByPetIdAsync(int petId);

    // Medication (MED-02)
    Task<Result<MedicationRecordDto>> CreateMedicationAsync(CreateMedicationDto dto, string vetUserId);
    Task<MedicationRecordDto?> GetMedicationByIdAsync(int id);
    Task<IEnumerable<MedicationRecordDto>> GetMedicationsByPetIdAsync(int petId);

    // Visit Notes (MED-03)
    Task<Result<VetVisitNoteDto>> CreateVisitNoteAsync(CreateVetVisitNoteDto dto, string vetUserId);
    Task<VetVisitNoteDto?> GetVisitNoteByIdAsync(int id);
    Task<IEnumerable<VetVisitNoteDto>> GetVisitNotesByPetIdAsync(int petId);

    // Combined timeline (MED-04, D-12)
    Task<IEnumerable<MedicalRecordSummaryDto>> GetMedicalHistoryAsync(int petId);
    Task<IEnumerable<MedicalRecordSummaryDto>> GetRecentRecordsAsync(int petId, int count = 5);
}
