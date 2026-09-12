namespace ClinicaPro.Client.Shared;

/// <summary>
/// Mantiene los mensajes visibles para el usuario en un lenguaje claro y evita
/// exponer detalles de implementación que solo son útiles para desarrollo.
/// </summary>
public static class MensajeUsuario
{
    public static string Limpiar(string? mensaje, string mensajePorDefecto = "No fue posible completar la acción. Intenta nuevamente.")
    {
        if (string.IsNullOrWhiteSpace(mensaje))
        {
            return mensajePorDefecto;
        }

        var texto = mensaje.Trim();

        // Mensajes conocidos del dominio que pueden llegar con nombres internos.
        if (Contiene(texto, "POST /api/admin/medicos"))
            return "Para crear un médico, utiliza la sección Médicos y horarios.";

        if (Contiene(texto, "rol Paciente") && Contiene(texto, "desde admin"))
            return "Los pacientes se administran desde la sección Pacientes.";

        if (Contiene(texto, "No existe el rol Médico"))
            return "No pudimos configurar la cuenta como médico. Intenta nuevamente.";

        if (Contiene(texto, "debe conservar al menos un rol"))
            return "La cuenta debe conservar al menos un tipo de acceso.";

        if (Contiene(texto, "quitarse el rol Administrador"))
            return "No puedes quitar tu propio acceso de Administración.";

        if (Contiene(texto, "asignar el rol Médico"))
            return "Para configurar la cuenta como médico, completa nombres, apellidos y número de colegiado.";

        if (Contiene(texto, "asignar el rol Paciente"))
            return "Para configurar la cuenta como paciente, completa nombres, apellidos y DPI.";

        if (Contiene(texto, "VigenteHasta") || Contiene(texto, "VigenteDesde"))
            return "La fecha final no puede ser anterior a la fecha inicial.";

        if (Contiene(texto, "El sexo debe ser M, F o X"))
            return "Selecciona una opción válida para el sexo.";

        if (Contiene(texto, "tipo clínico"))
            return "Selecciona un tipo de usuario válido.";

        if (Contiene(texto, "valor del parámetro"))
            return "Ingresa un valor.";

        if (Contiene(texto, "parámetro no existe"))
            return "Este ajuste ya no está disponible. Actualiza la página e intenta nuevamente.";

        if (Contiene(texto, "máximo de reprogramaciones está fijo"))
            return "El máximo de reprogramaciones está definido por la clínica y no puede modificarse aquí.";

        if (Contiene(texto, "perfil de paciente"))
            return "Tu cuenta aún no está configurada como paciente. Comunícate con la clínica.";

        if (Contiene(texto, "perfil de médico"))
            return "Tu cuenta aún no está configurada como médico. Comunícate con administración.";

        if (Contiene(texto, "paciente autenticado"))
            return "No puedes realizar esta acción sobre esa cita.";

        if (Contiene(texto, "rango del reporte") || Contiene(texto, "rango de agenda"))
            return "Revisa las fechas seleccionadas e intenta nuevamente.";

        if (Contiene(texto, "estado de notificación"))
            return "Selecciona un estado válido.";

        if (Contiene(texto, "rango de fechas de notificaciones"))
            return "Revisa las fechas seleccionadas e intenta nuevamente.";

        if (Contiene(texto, "política de seguridad"))
            return "La contraseña no cumple los requisitos indicados.";

        // Si el servidor devuelve un detalle claramente técnico, mostramos el
        // contexto amigable de la operación en vez del texto interno.
        if (EsDetalleTecnico(texto))
        {
            return mensajePorDefecto;
        }

        return texto;
    }

    private static bool EsDetalleTecnico(string texto)
    {
        string[] indicadores =
        [
            "/api/", "endpoint", "backend", "frontend", "base de datos", "database",
            "sql", "constraint", "foreign key", "primary key", "stack trace", "exception",
            "http status", "status code", "5xx", "4xx", "json", "dto", "guid", "system.",
            "tipo esperado:", "tipo de dato", "se espera int", "espera int", "regla de negocio",
            "reglas de negocio", "int32", "int64", "system.int", "boolean", "datetime", "timeonly",
            "nullable", "debe ser int", "valor int", "debe asociarse a un usuario", "modelstate",
            "validationproblem", "dbupdate", "entity framework", "controller", "middleware", "stacktrace",
            "request body", "response body", "content-type", "usuarioid", "pacienteid", "medicoid",
            "citaid", "autorizacionid", "la página debe ser"
        ];

        return indicadores.Any(indicador => Contiene(texto, indicador));
    }

    private static bool Contiene(string texto, string valor)
        => texto.Contains(valor, StringComparison.OrdinalIgnoreCase);
}
