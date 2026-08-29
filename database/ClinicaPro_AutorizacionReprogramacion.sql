-- Incremental Somee: no re-ejecutar ClinicaPro_BD_Somee.sql completo.
-- 1) Cola de autorización para la tercera reprogramación.
-- 2) El máximo de reprogramaciones queda fijo en 3 (deja de ser parámetro editable).

IF OBJECT_ID(N'dbo.AutorizacionesReprogramacion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AutorizacionesReprogramacion
    (
        AutorizacionReprogramacionId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_AutorizacionesReprogramacion PRIMARY KEY
            CONSTRAINT DF_AutorizacionesReprogramacion_Id DEFAULT NEWSEQUENTIALID(),
        CitaId              UNIQUEIDENTIFIER NOT NULL,
        SolicitadaPorUsuarioId UNIQUEIDENTIFIER NOT NULL,
        AutorizadaPorUsuarioId UNIQUEIDENTIFIER NULL,
        Estado              NVARCHAR(20) NOT NULL CONSTRAINT DF_AutorizacionesReprogramacion_Estado DEFAULT (N'Pendiente'),
        MotivoSolicitud     NVARCHAR(500) NOT NULL,
        MotivoDecision      NVARCHAR(500) NULL,
        CreatedAtUtc        DATETIME2(0) NOT NULL CONSTRAINT DF_AutorizacionesReprogramacion_Created DEFAULT SYSUTCDATETIME(),
        DecididaAtUtc       DATETIME2(0) NULL,
        CONSTRAINT CK_AutorizacionesReprogramacion_Estado CHECK (Estado IN (N'Pendiente', N'Aprobada', N'Rechazada', N'Usada')),
        CONSTRAINT FK_AutorizacionesReprogramacion_Citas FOREIGN KEY (CitaId)
            REFERENCES dbo.Citas(CitaId) ON DELETE NO ACTION,
        CONSTRAINT FK_AutorizacionesReprogramacion_Solicita FOREIGN KEY (SolicitadaPorUsuarioId)
            REFERENCES dbo.Usuarios(UsuarioId) ON DELETE NO ACTION,
        CONSTRAINT FK_AutorizacionesReprogramacion_Autoriza FOREIGN KEY (AutorizadaPorUsuarioId)
            REFERENCES dbo.Usuarios(UsuarioId) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_AutorizacionesReprogramacion_Cita'
      AND object_id = OBJECT_ID(N'dbo.AutorizacionesReprogramacion'))
    CREATE INDEX IX_AutorizacionesReprogramacion_Cita
    ON dbo.AutorizacionesReprogramacion (CitaId, CreatedAtUtc DESC);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_AutorizacionesReprogramacion_Pendiente'
      AND object_id = OBJECT_ID(N'dbo.AutorizacionesReprogramacion'))
    CREATE UNIQUE INDEX UX_AutorizacionesReprogramacion_Pendiente
    ON dbo.AutorizacionesReprogramacion (CitaId)
    WHERE Estado = N'Pendiente';

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_AutorizacionesReprogramacion_Aprobada'
      AND object_id = OBJECT_ID(N'dbo.AutorizacionesReprogramacion'))
    CREATE UNIQUE INDEX UX_AutorizacionesReprogramacion_Aprobada
    ON dbo.AutorizacionesReprogramacion (CitaId)
    WHERE Estado = N'Aprobada';

UPDATE dbo.Parametros
SET IsActive = 0,
    Descripcion = N'Regla fija en 3 (documento de arquitectura). No editable.'
WHERE Clave = N'Citas.MaximoReprogramaciones';
