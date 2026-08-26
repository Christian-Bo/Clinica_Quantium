using ClinicaPro.Application.Citas;
using ClinicaPro.Domain;

namespace ClinicaPro.UnitTests;

public sealed class HistorialCitaNarrativaTests
{
    [Fact]
    public void Redactar_Creacion_ExplicaQuienYCuando()
    {
        var texto = HistorialCitaNarrativa.Redactar(
            "Creacion",
            "Ana López",
            RolNombres.Paciente,
            null,
            CitaEstados.Solicitada,
            null,
            new DateTime(2026, 9, 11, 9, 0, 0),
            null,
            new DateTime(2026, 9, 11, 9, 30, 0),
            "Creación de solicitud de cita");

        Assert.Contains("Ana López (Paciente)", texto);
        Assert.Contains("11/09/2026 09:00", texto);
        Assert.Contains("09:30", texto);
    }

    [Fact]
    public void Redactar_Reprogramacion_IncluyeHorarioAnteriorYNuevo()
    {
        var texto = HistorialCitaNarrativa.Redactar(
            "Reprogramacion",
            "María Pérez",
            RolNombres.Secretaria,
            CitaEstados.Programada,
            CitaEstados.Programada,
            new DateTime(2026, 9, 11, 9, 0, 0),
            new DateTime(2026, 9, 12, 10, 0, 0),
            new DateTime(2026, 9, 11, 9, 30, 0),
            new DateTime(2026, 9, 12, 10, 30, 0),
            "El paciente pidió otro horario");

        Assert.Contains("María Pérez (Secretaria)", texto);
        Assert.Contains("11/09/2026 09:00", texto);
        Assert.Contains("12/09/2026 10:00", texto);
        Assert.Contains("El paciente pidió otro horario", texto);
    }

    [Fact]
    public void Redactar_CambioEstado_MuestraTransicion()
    {
        var texto = HistorialCitaNarrativa.Redactar(
            "CambioEstado",
            "Carlos Hernandez",
            RolNombres.Medico,
            CitaEstados.EnAtencion,
            CitaEstados.Atendida,
            null,
            null,
            null,
            null,
            "Consulta finalizada");

        Assert.Contains("Carlos Hernandez (Médico)", texto);
        Assert.Contains("En Atencion", texto);
        Assert.Contains("Atendida", texto);
    }
}
