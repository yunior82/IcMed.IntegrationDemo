namespace IcMed.IntegrationDemo.Domain.Entities;

/// <summary>
/// Represents a workplace (clinic location) exposed by icMED.
/// </summary>
/// <param name="Id">Unique identifier of the workplace.</param>
/// <param name="MedicalUnitId">Identifier of the medical unit owning the workplace.</param>
/// <param name="Name">Human-readable name of the workplace.</param>
public sealed record Workplace(long Id, long MedicalUnitId, string Name);
