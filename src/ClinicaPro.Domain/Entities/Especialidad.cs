using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Domain.Entities;

public sealed class Especialidad
{
    public const int NombreMaxLength = 100;
    public const int DescripcionMaxLength = 300;

    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string? Descripcion { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Especialidad()
    {
    }

    private Especialidad(
        Guid id,
        string nombre,
        string? descripcion,
        bool isActive,
        DateTime createdAtUtc)
    {
        Id = id;
        Nombre = nombre;
        Descripcion = descripcion;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
    }

    public static Especialidad Create(string nombre, string? descripcion = null)
    {
        var nombreNormalizado = NormalizarNombre(nombre);
        var descripcionNormalizada = NormalizarDescripcion(descripcion);

        return new Especialidad(
            Guid.NewGuid(),
            nombreNormalizado,
            descripcionNormalizada,
            isActive: true,
            createdAtUtc: DateTime.UtcNow);
    }

    private static string NormalizarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DomainException("El nombre de la especialidad es obligatorio.");
        }

        var valor = nombre.Trim();

        if (valor.Length > NombreMaxLength)
        {
            throw new DomainException($"El nombre de la especialidad no puede superar {NombreMaxLength} caracteres.");
        }

        return valor;
    }

    private static string? NormalizarDescripcion(string? descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            return null;
        }

        var valor = descripcion.Trim();

        if (valor.Length > DescripcionMaxLength)
        {
            throw new DomainException($"La descripción de la especialidad no puede superar {DescripcionMaxLength} caracteres.");
        }

        return valor;
    }

    public void Actualizar(string nombre, string? descripcion)
    {
        Nombre = NormalizarNombre(nombre);
        Descripcion = NormalizarDescripcion(descripcion);
    }

    public void CambiarActivo(bool activo) => IsActive = activo;
}
