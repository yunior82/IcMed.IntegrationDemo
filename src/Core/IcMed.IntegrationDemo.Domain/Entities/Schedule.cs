namespace IcMed.IntegrationDemo.Domain.Entities;

/// <summary>
/// Represents an available or unavailable interval within a schedule.
/// All values are Unix epoch milliseconds as expected by the icMED API.
/// </summary>
/// <param name="Date">The day represented as Unix epoch milliseconds.</param>
/// <param name="StartTime">Start timestamp of the interval (epoch ms).</param>
/// <param name="EndTime">End timestamp of the interval (epoch ms).</param>
public sealed record Interval(long Date, long StartTime, long EndTime);

/// <summary>
/// Represents the scheduling information for a physician as returned by icMED.
/// </summary>
/// <param name="SlotSpan">Appointment slot duration in minutes.</param>
/// <param name="Unavailable">Collection of unavailable intervals.</param>
/// <param name="Available">Collection of available intervals.</param>
public sealed record Schedule(int SlotSpan, IReadOnlyList<Interval> Unavailable, IReadOnlyList<Interval> Available);
