using ClinicaPro.Application.Common;
using ClinicaPro.Contracts.Appointments;
using ClinicaPro.Domain.Appointments;
using ClinicaPro.Domain.Common;

namespace ClinicaPro.Application.Appointments;

public sealed class RequestAppointmentHandler
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly ISchedulingRepository _schedulingRepository;
    private readonly IClock _clock;

    public RequestAppointmentHandler(
        IAppointmentRepository appointmentRepository,
        ISchedulingRepository schedulingRepository,
        IClock clock)
    {
        _appointmentRepository = appointmentRepository;
        _schedulingRepository = schedulingRepository;
        _clock = clock;
    }

    public async Task<AppointmentResponse> HandleAsync(
        RequestAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _schedulingRepository.PatientExistsAsync(request.PatientId, cancellationToken))
            throw new BusinessRuleException("El paciente indicado no existe o se encuentra inactivo.");

        var doctorId = await _schedulingRepository.GetPrimaryDoctorIdAsync(
            request.SpecialtyId,
            cancellationToken);

        if (doctorId is null)
            throw new BusinessRuleException("La especialidad no tiene un médico primario activo asignado.");

        AppointmentRules.ValidateFuture(request.Date, request.StartTime, _clock.LocalNow);

        var endTime = request.StartTime.AddMinutes(AppointmentRules.DefaultDurationMinutes);
        var schedules = await _schedulingRepository.GetActiveSchedulesAsync(
            doctorId.Value,
            cancellationToken);

        AppointmentRules.ValidateSchedule(request.Date, request.StartTime, endTime, schedules);

        var overlaps = await _appointmentRepository.HasActiveOverlapAsync(
            doctorId.Value,
            request.Date,
            request.StartTime,
            endTime,
            cancellationToken);

        if (overlaps)
            throw new BusinessRuleException("El horario acaba de ser reservado. Seleccione otro horario disponible.");

        var appointment = Appointment.Request(
            request.PatientId,
            doctorId.Value,
            request.SpecialtyId,
            request.Date,
            request.StartTime,
            AppointmentRules.DefaultDurationMinutes,
            request.Reason,
            _clock.UtcNow);

        await _appointmentRepository.AddAsync(appointment, cancellationToken);

        return new AppointmentResponse(
            appointment.Id,
            appointment.PatientId,
            appointment.DoctorId,
            appointment.SpecialtyId,
            appointment.Date,
            appointment.StartTime,
            appointment.EndTime,
            appointment.Status.ToString(),
            appointment.Reason,
            appointment.CreatedAtUtc);
    }
}
