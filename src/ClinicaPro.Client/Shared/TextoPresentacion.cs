using System;
using System.Collections.Generic;

namespace ClinicaPro.Client.Shared;

/// <summary>
/// Normaliza datos de texto que vienen del servidor antes de mostrarlos.
/// Evita que valores de ejemplo usados por herramientas de prueba (por ejemplo
/// "string") o marcadores técnicos terminen visibles para el usuario final.
/// </summary>
public static class TextoPresentacion
{
    private static readonly HashSet<string> MarcadoresSinContenido = new(StringComparer.OrdinalIgnoreCase)
    {
        "string",
        "null",
        "undefined",
        "<string>",
        "<null>"
    };

    public static bool TieneContenido(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        var texto = valor.Trim();
        var comparable = texto.Trim('"', '\'').Trim();
        return !MarcadoresSinContenido.Contains(comparable);
    }

    public static string Mostrar(string? valor, string textoVacio = "No especificado.")
        => TieneContenido(valor) ? valor!.Trim() : textoVacio;

    public static string? Opcional(string? valor)
        => TieneContenido(valor) ? valor!.Trim() : null;

    public static string MotivoConsulta(string? valor)
        => Mostrar(valor, "Sin motivo especificado.");

    public static string Alergias(string? valor)
        => Mostrar(valor, "Sin alergias registradas.");
}
