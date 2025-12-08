namespace IcMed.IntegrationDemo.Domain.Entities;

/// <summary>
/// Represents the payload required by icMED to create a new appointment.
/// </summary>
/// <param name="ConsultReason">Reason for the consultation.</param>
/// <param name="FirstName">Patient first name.</param>
/// <param name="LastName">Patient last name.</param>
/// <param name="Observations">Optional observations.</param>
/// <param name="PhoneNo">Patient phone number.</param>
/// <param name="From">Start date/time of the appointment (server local time accepted by upstream API).</param>
/// <param name="To">End date/time of the appointment.</param>
/// <param name="IsActive">Whether the appointment should be active.</param>
/// <param name="OfficeId">Office identifier.</param>
/// <param name="SubOfficeId">Office subdivision identifier.</param>
/// <param name="SpecialityId">Speciality identifier.</param>
/// <param name="WorkplaceId">Workplace identifier.</param>
/// <param name="PhysicianId">Physician identifier.</param>
/// <param name="PatientCode">Optional patient social identifier.</param>
public sealed record AppointmentRequest(
    string ConsultReason,
    string FirstName,
    string LastName,
    string Observations,
    string PhoneNo,
    DateTime From,
    DateTime To,
    bool IsActive,
    long OfficeId,
    long SubOfficeId,
    long SpecialityId,
    long WorkplaceId,
    long PhysicianId,
    string? PatientCode
);

/// <summary>
/// Represents the response returned by icMED after creating a new appointment.
/// </summary>
/// <param name="ConsultReason">Reason for the consultation.</param>
/// <param name="FirstName">Patient first name.</param>
/// <param name="LastName">Patient last name.</param>
/// <param name="Observations">Optional observations.</param>
/// <param name="PhoneNo">Patient phone number.</param>
/// <param name="From">Start date/time of the appointment.</param>
/// <param name="To">End date/time of the appointment.</param>
/// <param name="IsActive">Whether the appointment is active.</param>
/// <param name="OfficeId">Office identifier.</param>
/// <param name="SubOfficeId">Office subdivision identifier.</param>
/// <param name="SpecialityId">Speciality identifier.</param>
/// <param name="WorkplaceId">Workplace identifier.</param>
/// <param name="PhysicianId">Physician identifier.</param>
/// <param name="PatientCode">Optional patient social identifier.</param>
/// <param name="Status">Appointment status (2 = requested).</param>
/// <param name="Id">Server-generated appointment identifier.</param>
public sealed record AppointmentResponse(
    string ConsultReason,
    string FirstName,
    string LastName,
    string Observations,
    string PhoneNo,
    DateTime From,
    DateTime To,
    bool IsActive,
    long OfficeId,
    long SubOfficeId,
    long SpecialityId,
    long WorkplaceId,
    long PhysicianId,
    string? PatientCode,
    int Status,
    long Id
);
