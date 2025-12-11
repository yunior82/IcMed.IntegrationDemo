using IcMed.IntegrationDemo.Application.Abstractions;
using IcMed.IntegrationDemo.Domain.Entities;

namespace IcMed.IntegrationDemo.Infrastructure.Clients;

/// <summary>
/// Mock implementation of <see cref="IIcMedClient"/> that returns deterministic
/// in-memory data. Useful for demos and local development when the live icMED
/// API is not reachable.
/// </summary>
internal sealed class IcMedMockClient : IIcMedClient
{
    /// <inheritdoc />
    public Task<IReadOnlyList<Workplace>> GetWorkplacesAsync(CancellationToken ct)
    {
        IReadOnlyList<Workplace> list = new List<Workplace>
        {
            new(3, 7, "Workplace 1"),
            new (27, 7, "Workplace 1")
        };
        return Task.FromResult(list);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Speciality>> GetSpecialitiesAsync(long workplaceId, CancellationToken ct)
    {
        IReadOnlyList<Speciality> list = new List<Speciality>
        {
            new (16, workplaceId, 7, 1, "MEDICINA DE FAMILIE"),
            new (29, workplaceId, 7, 1, "PSIHIATRIE PEDIATRICA"),
            new (71, workplaceId, 7, 1, "MEDICINA GENERALA")
        };
        return Task.FromResult(list);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Physician>> GetPhysiciansAsync(long workplaceId, long specialityId, CancellationToken ct)
    {
        IReadOnlyList<Physician> list = new List<Physician>
        {
            new (6, "IONUT", "DOBRESCU", 15, "MEDICINA GENERALA", null, 1, 1, 2, 1, "medic.test", 1, "medic.test", true),
            new (522, "Dan", "Suciu", 15, "MEDICINA GENERALA", null, 1, 1, 10, 1, "medic.test", 1, "medic.test", false),
            new (22923, "GASTRO", "DEMO", 15, "MEDICINA GENERALA", null, 1, 1, 10, 1, "MedUnit", 1, "medic.test", false)
        };
        return Task.FromResult(list);
    }

    /// <inheritdoc />
    public Task<Schedule> GetScheduleAsync(long physicianId, long subOfficeId, long referenceDate, string currentView, CancellationToken ct)
    {
        var available = new List<Interval>
        {
            new(referenceDate - 300000000, referenceDate + 43200000, referenceDate + 45800000),
            new(referenceDate + 86400000, referenceDate + 88000000, referenceDate + 90000000)
        };
        var schedule = new Schedule(15, [], available);
        return Task.FromResult(schedule);
    }

    /// <inheritdoc />
    public Task<AppointmentResponse> CreateAppointmentAsync(AppointmentRequest request, CancellationToken ct)
    {
        var response = new AppointmentResponse(
            request.ConsultReason,
            request.FirstName,
            request.LastName,
            request.Observations,
            request.PhoneNo,
            request.From,
            request.To,
            request.IsActive,
            request.OfficeId,
            request.SubOfficeId,
            request.SpecialityId,
            request.WorkplaceId,
            request.PhysicianId,
            request.PatientCode,
            Status: 2,
            Id: Random.Shared.NextInt64(100000, 999999)
        );
        return Task.FromResult(response);
    }
}
