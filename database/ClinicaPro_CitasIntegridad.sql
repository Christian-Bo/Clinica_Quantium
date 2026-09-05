/*
    Infra: aplicar en Somee. NO volver a ejecutar ClinicaPro_BD_Somee.sql.

    1. Reescribe TR_Citas_Validaciones sin Especialidades (ya no existen).
    2. Impide que el mismo paciente tenga dos citas activas que se solapen
       (cualquier médico). El API ya lo valida; este trigger es la garantía
       si dos peticiones llegan a la vez.
*/

CREATE OR ALTER TRIGGER dbo.TR_Citas_Validaciones
ON dbo.Citas
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        LEFT JOIN deleted d ON d.CitaId = i.CitaId
        WHERE (d.CitaId IS NULL OR i.FechaHoraInicio <> d.FechaHoraInicio OR i.FechaHoraFin <> d.FechaHoraFin)
          AND i.FechaHoraInicio <= CONVERT(DATETIME2(0),
                SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'Central America Standard Time')
    )
    BEGIN
        THROW 51000, N'No se permite crear o reprogramar una cita en una fecha u hora pasada.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        LEFT JOIN dbo.Medicos m ON m.MedicoId = i.MedicoId AND m.IsActive = 1
        WHERE m.MedicoId IS NULL
          AND i.BloqueaHorario = 1
    )
    BEGIN
        THROW 51001, N'El médico seleccionado no existe o no está activo.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        WHERE i.BloqueaHorario = 1
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.Horarios h
              WHERE h.MedicoId = i.MedicoId
                AND h.IsActive = 1
                AND h.DiaSemana = CONVERT(TINYINT, (DATEDIFF(DAY, CONVERT(DATE,'19000101',112), CONVERT(DATE, i.FechaHoraInicio)) % 7) + 1)
                AND CAST(i.FechaHoraInicio AS TIME(0)) >= h.HoraInicio
                AND CAST(i.FechaHoraFin AS TIME(0)) <= h.HoraFin
                AND (h.VigenteDesde IS NULL OR CONVERT(DATE, i.FechaHoraInicio) >= h.VigenteDesde)
                AND (h.VigenteHasta IS NULL OR CONVERT(DATE, i.FechaHoraInicio) <= h.VigenteHasta)
          )
    )
    BEGIN
        THROW 51002, N'La cita está fuera del horario laboral configurado para el médico.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        JOIN dbo.Citas c
          ON c.MedicoId = i.MedicoId
         AND c.CitaId <> i.CitaId
         AND c.BloqueaHorario = 1
         AND i.BloqueaHorario = 1
         AND c.FechaHoraInicio < i.FechaHoraFin
         AND i.FechaHoraInicio < c.FechaHoraFin
    )
    BEGIN
        THROW 51003, N'Existe otra solicitud o cita activa que se solapa con el horario indicado.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        JOIN dbo.Citas c
          ON c.PacienteId = i.PacienteId
         AND c.CitaId <> i.CitaId
         AND c.BloqueaHorario = 1
         AND i.BloqueaHorario = 1
         AND c.FechaHoraInicio < i.FechaHoraFin
         AND i.FechaHoraInicio < c.FechaHoraFin
    )
    BEGIN
        THROW 51006, N'El paciente ya tiene otra cita activa que se solapa con el horario indicado.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        WHERE i.NumeroReprogramaciones = 3
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.UsuarioRoles ur
              JOIN dbo.Roles r ON r.RoleId = ur.RoleId
              WHERE ur.UsuarioId = i.AutorizacionTerceraPorUsuarioId
                AND r.NormalizedName = N'ADMINISTRADOR'
                AND r.IsActive = 1
          )
    )
    BEGIN
        THROW 51004, N'La tercera reprogramación requiere autorización de un Administrador.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON d.CitaId = i.CitaId
        WHERE i.Estado <> d.Estado
          AND NOT
          (
              (d.Estado = N'Solicitada'  AND i.Estado IN (N'Programada', N'Rechazada')) OR
              (d.Estado = N'Programada'  AND i.Estado IN (N'Confirmada', N'En Espera', N'Cancelada', N'No presentada')) OR
              (d.Estado = N'Confirmada'  AND i.Estado IN (N'En Espera', N'Cancelada', N'No presentada')) OR
              (d.Estado = N'En Espera'   AND i.Estado IN (N'En Atencion', N'Atendida', N'No presentada')) OR
              (d.Estado = N'En Atencion' AND i.Estado IN (N'Atendida'))
          )
    )
    BEGIN
        THROW 51005, N'Transición de estado no permitida por las reglas de negocio.', 1;
    END;
END;
GO
