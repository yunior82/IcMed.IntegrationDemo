namespace IcMed.IntegrationDemo.Domain.Entities;

/// <summary>
/// Represents a medical speciality available at a workplace in icMED.
/// </summary>
/// <param name="Id">Unique identifier of the speciality.</param>
/// <param name="WorkplaceId">Identifier of the workplace where the speciality is offered.</param>
/// <param name="MedicalUnitId">Identifier of the medical unit.</param>
/// <param name="OfficeId">Identifier of the office associated with the speciality.</param>
/// <param name="Name">Display name of the speciality.</param>
public sealed record Speciality(long Id, long WorkplaceId, long MedicalUnitId, long OfficeId, string Name);
