using ClinicaPro.Domain.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Persistence;

internal static class SqlServerExceptionMapper
{
    public static Exception Map(Exception exception)
    {
        for (var actual = exception; actual is not null; actual = actual.InnerException)
        {
            if (actual is SqlException sql)
            {
                if (sql.Number is >= 51000 and <= 51006 or 50999)
                {
                    return new DomainException(sql.Message);
                }

                if (sql.Number is 2601 or 2627)
                {
                    return new DomainException("Ya existe una cita activa en ese horario.");
                }

                if (sql.Number == 547)
                {
                    return new DomainException("La operación viola una regla de integridad de la base de datos.");
                }
            }
        }

        return exception;
    }
}
