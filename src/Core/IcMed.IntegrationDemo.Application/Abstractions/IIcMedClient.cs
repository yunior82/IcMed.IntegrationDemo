using IcMed.IntegrationDemo.Domain.Entities;

namespace IcMed.IntegrationDemo.Application.Abstractions;

/// <summary>
/// Abstraction over the icMED HTTP API used by the application.
/// Exposes read endpoints (workplaces, specialities, physicians, schedules)
/// and the command endpoint for creating an appointment.
/// </summary>
public interface IIcMedClient
{
    /// <summary>
    /// Retrieves all workplaces available to the current credentials.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of <see cref="Workplace"/> items.</returns>
    Task<IReadOnlyList<Workplace>> GetWorkplacesAsync(CancellationToken ct);

    /// <summary>
    /// Retrieves specialities available at a specific workplace.
    /// </summary>
    /// <param name="workplaceId">Identifier of the workplace.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of <see cref="Speciality"/> items.</returns>
    Task<IReadOnlyList<Speciality>> GetSpecialitiesAsync(long workplaceId, CancellationToken ct);

    /// <summary>
    /// Retrieves physicians for a workplace and speciality.
    /// </summary>
    /// <param name="workplaceId">Identifier of the workplace.</param>
    /// <param name="specialityId">Identifier of the speciality.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of <see cref="Physician"/> items.</returns>
    Task<IReadOnlyList<Physician>> GetPhysiciansAsync(long workplaceId, long specialityId, CancellationToken ct);

    /// <summary>
    /// Retrieves the schedule for a physician in a given timeframe.
    /// </summary>
    /// <param name="physicianId">Identifier of the physician.</param>
    /// <param name="subOfficeId">Identifier of the subdivision/office.</param>
    /// <param name="referenceDate">Unix epoch milliseconds indicating the reference date.</param>
    /// <param name="currentView">Either "day" or "week".</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="Schedule"/> describing available and unavailable intervals.</returns>
    Task<Schedule> GetScheduleAsync(long physicianId, long subOfficeId, long referenceDate, string currentView, CancellationToken ct);

    /// <summary>
    /// Creates a new appointment.
    /// </summary>
    /// <param name="request">Appointment payload as defined by icMED.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created appointment with server-assigned fields.</returns>
    Task<AppointmentResponse> CreateAppointmentAsync(AppointmentRequest request, CancellationToken ct);
}
