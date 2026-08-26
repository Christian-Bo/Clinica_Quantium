using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Domain.Entities;

public sealed class Parametro
{
    public string Clave { get; private set; } = null!;
    public string Valor { get; private set; } = null!;
    public string TipoDato { get; private set; } = null!;
    public string? Descripcion { get; private set; }
    public bool IsActive { get; private set; }

    private Parametro()
    {
    }

    public void CambiarValor(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DomainException("El valor del parámetro es obligatorio.");
        }

        Valor = valor.Trim();
    }
}
