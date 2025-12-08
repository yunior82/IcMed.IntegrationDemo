namespace IcMed.IntegrationDemo.Domain.Entities;

/// <summary>
/// Represents a physician available for appointments in icMED.
/// </summary>
/// <param name="Id">Unique identifier of the physician.</param>
/// <param name="FirstName">Physician first name.</param>
/// <param name="LastName">Physician last name.</param>
/// <param name="EstimatedDuration">Estimated visit duration in minutes.</param>
/// <param name="Speciality">Name of the speciality.</param>
/// <param name="Description">Optional free-form description (unused by API).</param>
/// <param name="SubOfficeId">Identifier of the office subdivision.</param>
/// <param name="OfficeId">Identifier of the office.</param>
/// <param name="GroupId">Identifier of the physician's group.</param>
/// <param name="MedicalUnitId">Identifier of the medical unit.</param>
/// <param name="MedicalUnitName">Display name of the medical unit.</param>
/// <param name="WorkplaceId">Identifier of the workplace.</param>
/// <param name="WorkplaceName">Display name of the workplace.</param>
/// <param name="IsHolder">Whether the physician is the holder of the subdivision.</param>
public sealed record Physician(
    long Id,
    string FirstName,
    string LastName,
    int EstimatedDuration,
    string Speciality,
    string? Description,
    long SubOfficeId,
    long OfficeId,
    long GroupId,
    long MedicalUnitId,
    string MedicalUnitName,
    long WorkplaceId,
    string WorkplaceName,
    bool IsHolder);
