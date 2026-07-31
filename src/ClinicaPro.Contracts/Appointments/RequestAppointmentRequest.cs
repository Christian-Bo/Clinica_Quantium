using System.ComponentModel.DataAnnotations;

namespace ClinicaPro.Contracts.Appointments;

public sealed class RequestAppointmentRequest
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid SpecialtyId { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required, StringLength(200, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;
}
