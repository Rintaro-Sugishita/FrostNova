namespace FrostNova.Scheduling.Engine;

public record ScheduleViolationInfo(
    object StepId,
    ScheduleViolationType Type,
    DateTime ConstraintDate,
    DateTime ActualDate
);
