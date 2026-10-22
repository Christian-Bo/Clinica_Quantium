/* ============================================================================
   CLINICA PRO - MODULO DE GESTION DE CITAS
   VERSION PARA SOMEE - SQL SERVER 2022 EXPRESS

   IMPORTANTE:
   - Esta version NO crea la base de datos.
   - Primero cree una base vacia desde el panel de Somee.
   - Ejecute este script estando conectado directamente a ESA base.
   - No contiene CREATE DATABASE ni USE ClinicaPro.
   - Mantiene las mismas tablas, relaciones, indices, triggers y seeds
     del modelo validado localmente.

   FORMAS DE EJECUTAR:
   A) Somee Control Panel > MS SQL > su base > Run scripts > From your computer.
   B) DBeaver/SSMS conectandose remotamente a la base que creo Somee.
   ============================================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;


-- La base ya debe existir en Somee y esta conexion debe apuntar directamente a ella.

/* ============================= 1. SEGURIDAD ============================== */

IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        RoleId              UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_Roles PRIMARY KEY
            CONSTRAINT DF_Roles_RoleId DEFAULT NEWSEQUENTIALID(),
        Name                NVARCHAR(100) NOT NULL,
        NormalizedName      NVARCHAR(100) NOT NULL,
        ConcurrencyStamp    NVARCHAR(100) NULL,
        IsActive            BIT NOT NULL CONSTRAINT DF_Roles_IsActive DEFAULT (1),
        CreatedAtUtc        DATETIME2(0) NOT NULL CONSTRAINT DF_Roles_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_Roles_Name UNIQUE (Name),
        CONSTRAINT UQ_Roles_NormalizedName UNIQUE (NormalizedName)
    );
END;

IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuarios
    (
        UsuarioId           UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_Usuarios PRIMARY KEY
            CONSTRAINT DF_Usuarios_UsuarioId DEFAULT NEWSEQUENTIALID(),
        UserName            NVARCHAR(256) NOT NULL,
        NormalizedUserName  NVARCHAR(256) NOT NULL,
        Email               NVARCHAR(256) NOT NULL,
        NormalizedEmail     NVARCHAR(256) NOT NULL,
        EmailConfirmed      BIT NOT NULL CONSTRAINT DF_Usuarios_EmailConfirmed DEFAULT (0),
        PasswordHash        NVARCHAR(500) NULL,
        SecurityStamp       NVARCHAR(100) NULL,
        ConcurrencyStamp    NVARCHAR(100) NULL,
        PhoneNumber         NVARCHAR(30) NULL,
        PhoneNumberConfirmed BIT NOT NULL CONSTRAINT DF_Usuarios_PhoneConfirmed DEFAULT (0),
        TwoFactorEnabled    BIT NOT NULL CONSTRAINT DF_Usuarios_TwoFactor DEFAULT (0),
        LockoutEnd          DATETIMEOFFSET(0) NULL,
        LockoutEnabled      BIT NOT NULL CONSTRAINT DF_Usuarios_LockoutEnabled DEFAULT (1),
        AccessFailedCount   INT NOT NULL CONSTRAINT DF_Usuarios_AccessFailedCount DEFAULT (0),
        IsActive            BIT NOT NULL CONSTRAINT DF_Usuarios_IsActive DEFAULT (1),
        MustChangePassword  BIT NOT NULL CONSTRAINT DF_Usuarios_MustChangePassword DEFAULT (0),
        CreatedAtUtc        DATETIME2(0) NOT NULL CONSTRAINT DF_Usuarios_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        UpdatedAtUtc        DATETIME2(0) NULL,
        CONSTRAINT UQ_Usuarios_NormalizedUserName UNIQUE (NormalizedUserName),
        CONSTRAINT CK_Usuarios_AccessFailedCount CHECK (AccessFailedCount >= 0)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Usuarios_NormalizedEmail' AND object_id = OBJECT_ID(N'dbo.Usuarios'))
    CREATE UNIQUE INDEX UX_Usuarios_NormalizedEmail ON dbo.Usuarios (NormalizedEmail);

IF OBJECT_ID(N'dbo.UsuarioRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuarioRoles
    (
        UsuarioId UNIQUEIDENTIFIER NOT NULL,
        RoleId    UNIQUEIDENTIFIER NOT NULL,
        AssignedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_UsuarioRoles_AssignedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_UsuarioRoles PRIMARY KEY (UsuarioId, RoleId),
        CONSTRAINT FK_UsuarioRoles_Usuarios FOREIGN KEY (UsuarioId)
            REFERENCES dbo.Usuarios(UsuarioId) ON DELETE NO ACTION,
        CONSTRAINT FK_UsuarioRoles_Roles FOREIGN KEY (RoleId)
            REFERENCES dbo.Roles(RoleId) ON DELETE NO ACTION
    );
END;

-- Tablas de soporte requeridas por ASP.NET Core Identity/IdentityDbContext.
-- Deben mapearse en EF Core si se conservan estos nombres en español.
IF OBJECT_ID(N'dbo.RolClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolClaims
    (
        Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RolClaims PRIMARY KEY,
        RoleId      UNIQUEIDENTIFIER NOT NULL,
        ClaimType   NVARCHAR(MAX) NULL,
        ClaimValue  NVARCHAR(MAX) NULL,
        CONSTRAINT FK_RolClaims_Roles FOREIGN KEY (RoleId)
            REFERENCES dbo.Roles(RoleId) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RolClaims_RoleId' AND object_id = OBJECT_ID(N'dbo.RolClaims'))
    CREATE INDEX IX_RolClaims_RoleId ON dbo.RolClaims (RoleId);

IF OBJECT_ID(N'dbo.UsuarioClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuarioClaims
    (
        Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UsuarioClaims PRIMARY KEY,
        UsuarioId   UNIQUEIDENTIFIER NOT NULL,
        ClaimType   NVARCHAR(MAX) NULL,
        ClaimValue  NVARCHAR(MAX) NULL,
        CONSTRAINT FK_UsuarioClaims_Usuarios FOREIGN KEY (UsuarioId)
            REFERENCES dbo.Usuarios(UsuarioId) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UsuarioClaims_UsuarioId' AND object_id = OBJECT_ID(N'dbo.UsuarioClaims'))
    CREATE INDEX IX_UsuarioClaims_UsuarioId ON dbo.UsuarioClaims (UsuarioId);

IF OBJECT_ID(N'dbo.UsuarioLogins', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuarioLogins
    (
        LoginProvider       NVARCHAR(128) NOT NULL,
        ProviderKey         NVARCHAR(128) NOT NULL,
        ProviderDisplayName NVARCHAR(MAX) NULL,
        UsuarioId           UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_UsuarioLogins PRIMARY KEY (LoginProvider, ProviderKey),
        CONSTRAINT FK_UsuarioLogins_Usuarios FOREIGN KEY (UsuarioId)
            REFERENCES dbo.Usuarios(UsuarioId) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UsuarioLogins_UsuarioId' AND object_id = OBJECT_ID(N'dbo.UsuarioLogins'))
    CREATE INDEX IX_UsuarioLogins_UsuarioId ON dbo.UsuarioLogins (UsuarioId);

IF OBJECT_ID(N'dbo.UsuarioTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuarioTokens
    (
        UsuarioId      UNIQUEIDENTIFIER NOT NULL,
        LoginProvider  NVARCHAR(128) NOT NULL,
        Name           NVARCHAR(128) NOT NULL,
        Value          NVARCHAR(MAX) NULL,
        CONSTRAINT PK_UsuarioTokens PRIMARY KEY (UsuarioId, LoginProvider, Name),
        CONSTRAINT FK_UsuarioTokens_Usuarios FOREIGN KEY (UsuarioId)
            REFERENCES dbo.Usuarios(UsuarioId) ON DELETE NO ACTION
    );
END;

/* ============================= 2. PERSONAS =============================== */

IF OBJECT_ID(N'dbo.Pacientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Pacientes
    (
        PacienteId          UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_Pacientes PRIMARY KEY
            CONSTRAINT DF_Pacientes_PacienteId DEFAULT NEWSEQUENTIALID(),
        UsuarioId           UNIQUEIDENTIFIER NOT NULL,
        Nombres             NVARCHAR(100) NOT NULL,
        Apellidos           NVARCHAR(100) NOT NULL,
        Documento           NVARCHAR(30) NULL,
        FechaNacimiento     DATE NULL,
        Telefono            NVARCHAR(30) NULL,
        Direccion           NVARCHAR(250) NULL,
        IsActive            BIT NOT NULL CONSTRAINT DF_Pacientes_IsActive DEFAULT (1),
        CreatedAtUtc        DATETIME2(0) NOT NULL CONSTRAINT DF_Pacientes_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_Pacientes_UsuarioId UNIQUE (UsuarioId),
        CONSTRAINT FK_Pacientes_Usuarios FOREIGN KEY (UsuarioId)
            REFERENCES dbo.Usuarios(UsuarioId) ON DELETE NO ACTION
    );
END;

IF OBJECT_ID(N'dbo.Medicos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Medicos
    (
        MedicoId            UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_Medicos PRIMARY KEY
            CONSTRAINT DF_Medicos_MedicoId DEFAULT NEWSEQUENTIALID(),
        UsuarioId           UNIQUEIDENTIFIER NOT NULL,
        Nombres             NVARCHAR(100) NOT NULL,
        Apellidos           NVARCHAR(100) NOT NULL,
        NumeroColegiado     NVARCHAR(50) NULL,
        Telefono            NVARCHAR(30) NULL,
        IsActive            BIT NOT NULL CONSTRAINT DF_Medicos_IsActive DEFAULT (1),
        CreatedAtUtc        DATETIME2(0) NOT NULL CONSTRAINT DF_Medicos_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_Medicos_UsuarioId UNIQUE (UsuarioId),
        CONSTRAINT FK_Medicos_Usuarios FOREIGN KEY (UsuarioId)
            REFERENCES dbo.Usuarios(UsuarioId) ON DELETE NO ACTION
    );
END;

/* =========================== 3. CATALOGOS MEDICOS ======================== */

IF OBJECT_ID(N'dbo.Especialidades', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Especialidades
    (
        EspecialidadId      UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_Especialidades PRIMARY KEY
            CONSTRAINT DF_Especialidades_EspecialidadId DEFAULT NEWSEQUENTIALID(),
        Nombre              NVARCHAR(100) NOT NULL,
        Descripcion         NVARCHAR(300) NULL,
        IsActive            BIT NOT NULL CONSTRAINT DF_Especialidades_IsActive DEFAULT (1),
        CreatedAtUtc        DATETIME2(0) NOT NULL CONSTRAINT DF_Especialidades_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_Especialidades_Nombre UNIQUE (Nombre)
    );
END;

IF OBJECT_ID(N'dbo.MedicoEspecialidad', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MedicoEspecialidad
    (
        MedicoId            UNIQUEIDENTIFIER NOT NULL,
        EspecialidadId      UNIQUEIDENTIFIER NOT NULL,
        EsPrimario          BIT NOT NULL CONSTRAINT DF_MedicoEspecialidad_EsPrimario DEFAULT (0),
        IsActive            BIT NOT NULL CONSTRAINT DF_MedicoEspecialidad_IsActive DEFAULT (1),
        CreatedAtUtc        DATETIME2(0) NOT NULL CONSTRAINT DF_MedicoEspecialidad_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_MedicoEspecialidad PRIMARY KEY (MedicoId, EspecialidadId),
        CONSTRAINT FK_MedicoEspecialidad_Medicos FOREIGN KEY (MedicoId)
            REFERENCES dbo.Medicos(MedicoId) ON DELETE NO ACTION,
        CONSTRAINT FK_MedicoEspecialidad_Especialidades FOREIGN KEY (EspecialidadId)
            REFERENCES dbo.Especialidades(EspecialidadId) ON DELETE NO ACTION
    );
END;

-- Fase 1: una especialidad solo puede tener un médico primario activo.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_MedicoEspecialidad_PrimarioActivo' AND object_id = OBJECT_ID(N'dbo.MedicoEspecialidad'))
    CREATE UNIQUE INDEX UX_MedicoEspecialidad_PrimarioActivo
    ON dbo.MedicoEspecialidad (EspecialidadId)
    WHERE EsPrimario = 1 AND IsActive = 1;

IF OBJECT_ID(N'dbo.Horarios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Horarios
    (
        HorarioId           UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_Horarios PRIMARY KEY
            CONSTRAINT DF_Horarios_HorarioId DEFAULT NEWSEQUENTIALID(),
        MedicoId            UNIQUEIDENTIFIER NOT NULL,
        DiaSemana           TINYINT NOT NULL, -- 1=Lunes ... 7=Domingo
        HoraInicio          TIME(0) NOT NULL,
        HoraFin             TIME(0) NOT NULL,
        VigenteDesde        DATE NULL,
        VigenteHasta        DATE NULL,
        IsActive            BIT NOT NULL CONSTRAINT DF_Horarios_IsActive DEFAULT (1),
        CreatedAtUtc        DATETIME2(0) NOT NULL CONSTRAINT DF_Horarios_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_Horarios_DiaSemana CHECK (DiaSemana BETWEEN 1 AND 7),
        CONSTRAINT CK_Horarios_Rango CHECK (HoraFin > HoraInicio),
        CONSTRAINT CK_Horarios_Vigencia CHECK (VigenteHasta IS NULL OR VigenteDesde IS NULL OR VigenteHasta >= VigenteDesde),
        CONSTRAINT FK_Horarios_Medicos FOREIGN KEY (MedicoId)
            REFERENCES dbo.Medicos(MedicoId) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Horarios_MedicoId_DiaSemana' AND object_id = OBJECT_ID(N'dbo.Horarios'))
    CREATE INDEX IX_Horarios_MedicoId_DiaSemana
    ON dbo.Horarios (MedicoId, DiaSemana, IsActive, HoraInicio, HoraFin);

/* ================================ 4. CITAS ================================ */

IF OBJECT_ID(N'dbo.Citas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Citas
    (
        CitaId              UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_Citas PRIMARY KEY
            CONSTRAINT DF_Citas_CitaId DEFAULT NEWSEQUENTIALID(),
        PacienteId          UNIQUEIDENTIFIER NOT NULL,
        MedicoId            UNIQUEIDENTIFIER NOT NULL,
        EspecialidadId      UNIQUEIDENTIFIER NOT NULL,
        FechaHoraInicio     DATETIME2(0) NOT NULL,
        FechaHoraFin        DATETIME2(0) NOT NULL,
        MotivoConsulta      NVARCHAR(500) NOT NULL,
        Estado              NVARCHAR(20) NOT NULL CONSTRAINT DF_Citas_Estado DEFAULT (N'Solicitada'),
        NumeroReprogramaciones TINYINT NOT NULL CONSTRAINT DF_Citas_Reprogramaciones DEFAULT (0),
        AutorizacionTerceraPorUsuarioId UNIQUEIDENTIFIER NULL,
        CreadaPorUsuarioId  UNIQUEIDENTIFIER NOT NULL,
        SecretariaResponsableId UNIQUEIDENTIFIER NULL,
        CreatedAtUtc        DATETIME2(0) NOT NULL CONSTRAINT DF_Citas_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        UpdatedAtUtc        DATETIME2(0) NULL,
        RowVersion          ROWVERSION NOT NULL,
        BloqueaHorario AS CONVERT(BIT,
            CASE WHEN Estado IN (N'Solicitada', N'Programada', N'Confirmada', N'En Espera', N'En Atencion')
                 THEN 1 ELSE 0 END) PERSISTED,

        CONSTRAINT CK_Citas_Rango CHECK (FechaHoraFin > FechaHoraInicio),
        CONSTRAINT CK_Citas_MismoDia CHECK (CONVERT(DATE, FechaHoraInicio) = CONVERT(DATE, FechaHoraFin)),
        CONSTRAINT CK_Citas_Motivo CHECK (LEN(LTRIM(RTRIM(MotivoConsulta))) >= 5),
        CONSTRAINT CK_Citas_Estado CHECK (Estado IN
            (N'Solicitada', N'Programada', N'Confirmada', N'En Espera', N'En Atencion',
             N'Atendida', N'Cancelada', N'No presentada', N'Rechazada')),
        CONSTRAINT CK_Citas_Reprogramaciones CHECK (NumeroReprogramaciones BETWEEN 0 AND 3),
        CONSTRAINT CK_Citas_TerceraReprogramacion CHECK
            (NumeroReprogramaciones < 3 OR AutorizacionTerceraPorUsuarioId IS NOT NULL),

        CONSTRAINT FK_Citas_Pacientes FOREIGN KEY (PacienteId)
            REFERENCES dbo.Pacientes(PacienteId) ON DELETE NO ACTION,
        CONSTRAINT FK_Citas_Medicos FOREIGN KEY (MedicoId)
            REFERENCES dbo.Medicos(MedicoId) ON DELETE NO ACTION,
        CONSTRAINT FK_Citas_Especialidades FOREIGN KEY (EspecialidadId)
            REFERENCES dbo.Especialidades(EspecialidadId) ON DELETE NO ACTION,
        CONSTRAINT FK_Citas_CreadaPor FOREIGN KEY (CreadaPorUsuarioId)
            REFERENCES dbo.Usuarios(UsuarioId) ON DELETE NO ACTION,
        CONSTRAINT FK_Citas_SecretariaResponsable FOREIGN KEY (SecretariaResponsableId)
            REFERENCES dbo.Usuarios(UsuarioId) ON DELETE NO ACTION,
        CONSTRAINT FK_Citas_AutorizacionTercera FOREIGN KEY (AutorizacionTerceraPorUsuarioId)
            REFERENCES dbo.Usuarios(UsuarioId) ON DELETE NO ACTION
    );
END;

-- Índices solicitados.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Citas_MedicoId_Fecha' AND object_id = OBJECT_ID(N'dbo.Citas'))
    CREATE INDEX IX_Citas_MedicoId_Fecha
    ON dbo.Citas (MedicoId, FechaHoraInicio)
    INCLUDE (FechaHoraFin, Estado, PacienteId, EspecialidadId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Citas_Estado' AND object_id = OBJECT_ID(N'dbo.Citas'))
    CREATE INDEX IX_Citas_Estado
    ON dbo.Citas (Estado, FechaHoraInicio)
    INCLUDE (MedicoId, PacienteId, EspecialidadId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Citas_PacienteId_Fecha' AND object_id = OBJECT_ID(N'dbo.Citas'))
    CREATE INDEX IX_Citas_PacienteId_Fecha
    ON dbo.Citas (PacienteId, FechaHoraInicio DESC);

-- Evita duplicados exactos para estados que bloquean agenda.
-- El filtro usa Estado directamente: SQL Server no permite una columna calculada
-- dentro de la definición WHERE de un índice filtrado.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Citas_Medico_HorarioActivo' AND object_id = OBJECT_ID(N'dbo.Citas'))
    CREATE UNIQUE INDEX UX_Citas_Medico_HorarioActivo
    ON dbo.Citas (MedicoId, FechaHoraInicio, FechaHoraFin)
    WHERE Estado IN (N'Solicitada', N'Programada', N'Confirmada', N'En Espera', N'En Atencion');

/* ============================= 5. TRAZABILIDAD =========================== */

IF OBJECT_ID(N'dbo.HistorialCitas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HistorialCitas
    (
        HistorialCitaId     BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HistorialCitas PRIMARY KEY,
        CitaId              UNIQUEIDENTIFIER NOT NULL,
        UsuarioId           UNIQUEIDENTIFIER NOT NULL,
        TipoCambio          NVARCHAR(30) NOT NULL,
        EstadoAnterior      NVARCHAR(20) NULL,
        EstadoNuevo         NVARCHAR(20) NULL,
        FechaHoraInicioAnterior DATETIME2(0) NULL,
        FechaHoraInicioNueva    DATETIME2(0) NULL,
        FechaHoraFinAnterior    DATETIME2(0) NULL,
        FechaHoraFinNueva       DATETIME2(0) NULL,
        Motivo              NVARCHAR(500) NOT NULL,
        FechaCambioUtc      DATETIME2(0) NOT NULL CONSTRAINT DF_HistorialCitas_Fecha DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_HistorialCitas_TipoCambio CHECK (TipoCambio IN
            (N'Creacion', N'CambioEstado', N'Reprogramacion', N'Cancelacion', N'Autorizacion')),
        CONSTRAINT FK_HistorialCitas_Citas FOREIGN KEY (CitaId)
            REFERENCES dbo.Citas(CitaId) ON DELETE NO ACTION,
        CONSTRAINT FK_HistorialCitas_Usuarios FOREIGN KEY (UsuarioId)
            REFERENCES dbo.Usuarios(UsuarioId) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HistorialCitas_CitaId' AND object_id = OBJECT_ID(N'dbo.HistorialCitas'))
    CREATE INDEX IX_HistorialCitas_CitaId
    ON dbo.HistorialCitas (CitaId, FechaCambioUtc DESC);

/* ============================= 6. NOTIFICACIONES ========================== */

IF OBJECT_ID(N'dbo.Notificaciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notificaciones
    (
        NotificacionId      BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Notificaciones PRIMARY KEY,
        CitaId              UNIQUEIDENTIFIER NULL,
        PacienteId          UNIQUEIDENTIFIER NOT NULL,
        Canal               NVARCHAR(20) NOT NULL,
        Tipo                NVARCHAR(50) NOT NULL,
        Destinatario        NVARCHAR(256) NOT NULL,
        Asunto              NVARCHAR(200) NULL,
        Contenido           NVARCHAR(MAX) NOT NULL,
        Estado              NVARCHAR(20) NOT NULL CONSTRAINT DF_Notificaciones_Estado DEFAULT (N'Pendiente'),
        NumeroIntentos      INT NOT NULL CONSTRAINT DF_Notificaciones_Intentos DEFAULT (0),
        ProximoIntentoUtc   DATETIME2(0) NULL,
        EnviadaAtUtc        DATETIME2(0) NULL,
        UltimoError         NVARCHAR(1000) NULL,
        CreatedAtUtc        DATETIME2(0) NOT NULL CONSTRAINT DF_Notificaciones_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_Notificaciones_Canal CHECK (Canal IN (N'Email', N'WhatsApp')),
        CONSTRAINT CK_Notificaciones_Estado CHECK (Estado IN (N'Pendiente', N'Procesando', N'Enviada', N'Fallida')),
        CONSTRAINT CK_Notificaciones_Intentos CHECK (NumeroIntentos >= 0),
        CONSTRAINT FK_Notificaciones_Citas FOREIGN KEY (CitaId)
            REFERENCES dbo.Citas(CitaId) ON DELETE NO ACTION,
        CONSTRAINT FK_Notificaciones_Pacientes FOREIGN KEY (PacienteId)
            REFERENCES dbo.Pacientes(PacienteId) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Notificaciones_Estado' AND object_id = OBJECT_ID(N'dbo.Notificaciones'))
    CREATE INDEX IX_Notificaciones_Estado
    ON dbo.Notificaciones (Estado, ProximoIntentoUtc, CreatedAtUtc);

IF OBJECT_ID(N'dbo.IntentosNotificacion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IntentosNotificacion
    (
        IntentoNotificacionId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IntentosNotificacion PRIMARY KEY,
        NotificacionId      BIGINT NOT NULL,
        FechaIntentoUtc     DATETIME2(0) NOT NULL CONSTRAINT DF_IntentosNotificacion_Fecha DEFAULT SYSUTCDATETIME(),
        Exitoso             BIT NOT NULL,
        CodigoProveedor     NVARCHAR(100) NULL,
        RespuestaProveedor  NVARCHAR(1000) NULL,
        CONSTRAINT FK_IntentosNotificacion_Notificaciones FOREIGN KEY (NotificacionId)
            REFERENCES dbo.Notificaciones(NotificacionId) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_IntentosNotificacion_NotificacionId' AND object_id = OBJECT_ID(N'dbo.IntentosNotificacion'))
    CREATE INDEX IX_IntentosNotificacion_NotificacionId
    ON dbo.IntentosNotificacion (NotificacionId, FechaIntentoUtc DESC);

/* ============================== 7. AUDITORIA =============================== */

IF OBJECT_ID(N'dbo.Auditoria', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Auditoria
    (
        AuditoriaId         BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Auditoria PRIMARY KEY,
        UsuarioId           UNIQUEIDENTIFIER NULL,
        Accion              NVARCHAR(100) NOT NULL,
        Entidad             NVARCHAR(100) NOT NULL,
        EntidadId           NVARCHAR(100) NULL,
        Detalle             NVARCHAR(MAX) NULL,
        DireccionIp         NVARCHAR(45) NULL,
        CorrelationId       NVARCHAR(100) NULL,
        FechaUtc            DATETIME2(0) NOT NULL CONSTRAINT DF_Auditoria_Fecha DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Auditoria_Usuarios FOREIGN KEY (UsuarioId)
            REFERENCES dbo.Usuarios(UsuarioId) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Auditoria_Entidad_Fecha' AND object_id = OBJECT_ID(N'dbo.Auditoria'))
    CREATE INDEX IX_Auditoria_Entidad_Fecha
    ON dbo.Auditoria (Entidad, FechaUtc DESC);

/* ============================ 8. PARAMETROS ================================ */

IF OBJECT_ID(N'dbo.Parametros', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Parametros
    (
        Clave               NVARCHAR(100) NOT NULL CONSTRAINT PK_Parametros PRIMARY KEY,
        Valor               NVARCHAR(500) NOT NULL,
        TipoDato            NVARCHAR(20) NOT NULL,
        Descripcion         NVARCHAR(300) NULL,
        IsActive            BIT NOT NULL CONSTRAINT DF_Parametros_IsActive DEFAULT (1),
        UpdatedByUsuarioId  UNIQUEIDENTIFIER NULL,
        UpdatedAtUtc        DATETIME2(0) NOT NULL CONSTRAINT DF_Parametros_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_Parametros_TipoDato CHECK (TipoDato IN (N'Int', N'Decimal', N'Bool', N'String', N'Time')),
        CONSTRAINT FK_Parametros_Usuarios FOREIGN KEY (UpdatedByUsuarioId)
            REFERENCES dbo.Usuarios(UsuarioId) ON DELETE NO ACTION
    );
END;

/* ========================= 9. REGLAS EN BASE DE DATOS ===================== */

-- Verificación explícita de la zona horaria usada por la clínica.
IF NOT EXISTS (SELECT 1 FROM sys.time_zone_info WHERE name = N'Central America Standard Time')
BEGIN
    THROW 50999, N'La instancia SQL Server no tiene disponible la zona horaria Central America Standard Time.', 1;
END;


-- Valida relación médico-especialidad, horario laboral, solapamiento y
-- autorización administrativa en la tercera reprogramación.
EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_Citas_Validaciones
ON dbo.Citas
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- En creación o reprogramación, la fecha/hora debe ser futura.
    -- La clínica opera en Guatemala (Central America Standard Time).
    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        LEFT JOIN deleted d ON d.CitaId = i.CitaId
        WHERE (d.CitaId IS NULL OR i.FechaHoraInicio <> d.FechaHoraInicio OR i.FechaHoraFin <> d.FechaHoraFin)
          AND i.FechaHoraInicio <= CONVERT(DATETIME2(0),
                SYSUTCDATETIME() AT TIME ZONE ''UTC'' AT TIME ZONE ''Central America Standard Time'')
    )
    BEGIN
        THROW 51000, N''No se permite crear o reprogramar una cita en una fecha u hora pasada.'', 1;
    END;

    -- Médico y especialidad deben estar activos y relacionados.
    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        LEFT JOIN dbo.Medicos m ON m.MedicoId = i.MedicoId AND m.IsActive = 1
        LEFT JOIN dbo.Especialidades e ON e.EspecialidadId = i.EspecialidadId AND e.IsActive = 1
        LEFT JOIN dbo.MedicoEspecialidad me
               ON me.MedicoId = i.MedicoId
              AND me.EspecialidadId = i.EspecialidadId
              AND me.IsActive = 1
        WHERE m.MedicoId IS NULL OR e.EspecialidadId IS NULL OR me.MedicoId IS NULL
    )
    BEGIN
        THROW 51001, N''El médico y la especialidad deben estar activos y asociados.'', 1;
    END;

    -- Citas activas deben pertenecer al horario laboral configurado.
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
                AND h.DiaSemana = CONVERT(TINYINT, (DATEDIFF(DAY, CONVERT(DATE,''19000101'',112), CONVERT(DATE, i.FechaHoraInicio)) % 7) + 1)
                AND CAST(i.FechaHoraInicio AS TIME(0)) >= h.HoraInicio
                AND CAST(i.FechaHoraFin AS TIME(0)) <= h.HoraFin
                AND (h.VigenteDesde IS NULL OR CONVERT(DATE, i.FechaHoraInicio) >= h.VigenteDesde)
                AND (h.VigenteHasta IS NULL OR CONVERT(DATE, i.FechaHoraInicio) <= h.VigenteHasta)
          )
    )
    BEGIN
        THROW 51002, N''La cita está fuera del horario laboral configurado para el médico.'', 1;
    END;

    -- Impide intervalos superpuestos en estados que bloquean agenda.
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
        THROW 51003, N''Existe otra solicitud o cita activa que se solapa con el horario indicado.'', 1;
    END;

    -- La tercera reprogramación requiere un usuario con rol Administrador.
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
                AND r.NormalizedName = N''ADMINISTRADOR''
                AND r.IsActive = 1
          )
    )
    BEGIN
        THROW 51004, N''La tercera reprogramación requiere autorización de un Administrador.'', 1;
    END;

    -- Flujo de estados permitido. Reprogramar conserva el estado actual.
    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON d.CitaId = i.CitaId
        WHERE i.Estado <> d.Estado
          AND NOT
          (
              (d.Estado = N''Solicitada''  AND i.Estado IN (N''Programada'', N''Rechazada'')) OR
              (d.Estado = N''Programada''  AND i.Estado IN (N''Confirmada'', N''En Espera'', N''Cancelada'', N''No presentada'')) OR
              (d.Estado = N''Confirmada''  AND i.Estado IN (N''En Espera'', N''Cancelada'', N''No presentada'')) OR
              (d.Estado = N''En Espera''   AND i.Estado IN (N''En Atencion'', N''Atendida'', N''No presentada'')) OR
              (d.Estado = N''En Atencion'' AND i.Estado IN (N''Atendida''))
          )
    )
    BEGIN
        THROW 51005, N''Transición de estado no permitida por las reglas de negocio.'', 1;
    END;
END;');

-- Historial automático. La aplicación debe establecer:
-- EXEC sys.sp_set_session_context @key=N'UsuarioId', @value=@UsuarioId;
-- EXEC sys.sp_set_session_context @key=N'MotivoCambio', @value=@Motivo;
EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_Citas_Historial
ON dbo.Citas
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UsuarioContexto UNIQUEIDENTIFIER = TRY_CONVERT(UNIQUEIDENTIFIER, SESSION_CONTEXT(N''UsuarioId''));
    DECLARE @MotivoContexto NVARCHAR(500) = TRY_CONVERT(NVARCHAR(500), SESSION_CONTEXT(N''MotivoCambio''));

    -- Creación de solicitud: usa el usuario creador si no existe contexto.
    INSERT INTO dbo.HistorialCitas
    (
        CitaId, UsuarioId, TipoCambio, EstadoAnterior, EstadoNuevo,
        FechaHoraInicioAnterior, FechaHoraInicioNueva,
        FechaHoraFinAnterior, FechaHoraFinNueva,
        Motivo
    )
    SELECT
        i.CitaId,
        COALESCE(@UsuarioContexto, i.CreadaPorUsuarioId),
        N''Creacion'',
        NULL,
        i.Estado,
        NULL,
        i.FechaHoraInicio,
        NULL,
        i.FechaHoraFin,
        COALESCE(@MotivoContexto, N''Creación de solicitud de cita'')
    FROM inserted i
    LEFT JOIN deleted d ON d.CitaId = i.CitaId
    WHERE d.CitaId IS NULL;

    -- Cambios de estado y/o reprogramación.
    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON d.CitaId = i.CitaId
        WHERE i.Estado <> d.Estado
           OR i.FechaHoraInicio <> d.FechaHoraInicio
           OR i.FechaHoraFin <> d.FechaHoraFin
    )
    BEGIN
        IF @UsuarioContexto IS NULL
            THROW 51006, N''Para cambiar estado o reprogramar debe establecer SESSION_CONTEXT UsuarioId.'', 1;

        INSERT INTO dbo.HistorialCitas
        (
            CitaId, UsuarioId, TipoCambio, EstadoAnterior, EstadoNuevo,
            FechaHoraInicioAnterior, FechaHoraInicioNueva,
            FechaHoraFinAnterior, FechaHoraFinNueva,
            Motivo
        )
        SELECT
            i.CitaId,
            @UsuarioContexto,
            CASE
                WHEN i.FechaHoraInicio <> d.FechaHoraInicio OR i.FechaHoraFin <> d.FechaHoraFin THEN N''Reprogramacion''
                WHEN i.Estado = N''Cancelada'' THEN N''Cancelacion''
                ELSE N''CambioEstado''
            END,
            d.Estado,
            i.Estado,
            d.FechaHoraInicio,
            i.FechaHoraInicio,
            d.FechaHoraFin,
            i.FechaHoraFin,
            COALESCE(NULLIF(LTRIM(RTRIM(@MotivoContexto)), N''''), N''Cambio operativo de la cita'')
        FROM inserted i
        JOIN deleted d ON d.CitaId = i.CitaId
        WHERE i.Estado <> d.Estado
           OR i.FechaHoraInicio <> d.FechaHoraInicio
           OR i.FechaHoraFin <> d.FechaHoraFin;
    END;
END;');

/* ================================ 10. SEEDS ================================ */

DECLARE @RoleAdministrador UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000001';
DECLARE @RoleSecretaria    UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000002';
DECLARE @RoleMedico        UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000003';
DECLARE @RolePaciente      UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000004';

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE NormalizedName = N'ADMINISTRADOR')
    INSERT dbo.Roles (RoleId, Name, NormalizedName, ConcurrencyStamp)
    VALUES (@RoleAdministrador, N'Administrador', N'ADMINISTRADOR', CONVERT(NVARCHAR(100), NEWID()));

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE NormalizedName = N'SECRETARIA')
    INSERT dbo.Roles (RoleId, Name, NormalizedName, ConcurrencyStamp)
    VALUES (@RoleSecretaria, N'Secretaria', N'SECRETARIA', CONVERT(NVARCHAR(100), NEWID()));

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE NormalizedName = N'MEDICO')
    INSERT dbo.Roles (RoleId, Name, NormalizedName, ConcurrencyStamp)
    VALUES (@RoleMedico, N'Medico', N'MEDICO', CONVERT(NVARCHAR(100), NEWID()));

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE NormalizedName = N'PACIENTE')
    INSERT dbo.Roles (RoleId, Name, NormalizedName, ConcurrencyStamp)
    VALUES (@RolePaciente, N'Paciente', N'PACIENTE', CONVERT(NVARCHAR(100), NEWID()));

DECLARE @EspMedGeneral UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000001';
DECLARE @EspCardio     UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000002';
DECLARE @EspDerma      UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000003';
DECLARE @EspNutricion  UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000004';
DECLARE @EspNaturista  UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000005';

IF NOT EXISTS (SELECT 1 FROM dbo.Especialidades WHERE Nombre = N'Medicina General')
    INSERT dbo.Especialidades (EspecialidadId, Nombre) VALUES (@EspMedGeneral, N'Medicina General');
IF NOT EXISTS (SELECT 1 FROM dbo.Especialidades WHERE Nombre = N'Cardiología')
    INSERT dbo.Especialidades (EspecialidadId, Nombre) VALUES (@EspCardio, N'Cardiología');
IF NOT EXISTS (SELECT 1 FROM dbo.Especialidades WHERE Nombre = N'Dermatología')
    INSERT dbo.Especialidades (EspecialidadId, Nombre) VALUES (@EspDerma, N'Dermatología');
IF NOT EXISTS (SELECT 1 FROM dbo.Especialidades WHERE Nombre = N'Nutrición')
    INSERT dbo.Especialidades (EspecialidadId, Nombre) VALUES (@EspNutricion, N'Nutrición');
IF NOT EXISTS (SELECT 1 FROM dbo.Especialidades WHERE Nombre = N'Medicina Naturista')
    INSERT dbo.Especialidades (EspecialidadId, Nombre) VALUES (@EspNaturista, N'Medicina Naturista');

-- Usuario administrador inicial.
-- Password: Admin123!
-- PasswordHash compatible con ASP.NET Core Identity PasswordHasher v3.
-- Se fuerza cambio de contraseña al primer acceso.
DECLARE @AdminUsuarioId UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000001';
DECLARE @AdminRoleId UNIQUEIDENTIFIER = (SELECT RoleId FROM dbo.Roles WHERE NormalizedName = N'ADMINISTRADOR');

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE NormalizedEmail = N'ADMIN@CLINICA.COM')
BEGIN
    INSERT dbo.Usuarios
    (
        UsuarioId, UserName, NormalizedUserName, Email, NormalizedEmail,
        EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
        IsActive, MustChangePassword
    )
    VALUES
    (
        @AdminUsuarioId,
        N'admin@clinica.com',
        N'ADMIN@CLINICA.COM',
        N'admin@clinica.com',
        N'ADMIN@CLINICA.COM',
        1,
        N'AQAAAAIAAYagAAAAEG+cDCXzmTtp5fFSENbBU7XgLgiehUePKtAWfMYj9Qs/WJXJ7va1x+d6S4TF9pCJzA==',
        CONVERT(NVARCHAR(100), NEWID()),
        CONVERT(NVARCHAR(100), NEWID()),
        1,
        1
    );
END;

SET @AdminUsuarioId = (SELECT UsuarioId FROM dbo.Usuarios WHERE NormalizedEmail = N'ADMIN@CLINICA.COM');
IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioRoles WHERE UsuarioId = @AdminUsuarioId AND RoleId = @AdminRoleId)
    INSERT dbo.UsuarioRoles (UsuarioId, RoleId) VALUES (@AdminUsuarioId, @AdminRoleId);

-- Parámetros iniciales del módulo de citas.
IF NOT EXISTS (SELECT 1 FROM dbo.Parametros WHERE Clave = N'Citas.HorasMinimasCancelacion')
    INSERT dbo.Parametros (Clave, Valor, TipoDato, Descripcion)
    VALUES (N'Citas.HorasMinimasCancelacion', N'2', N'Int', N'Horas mínimas de anticipación para cancelar sin registrar inasistencia.');

IF NOT EXISTS (SELECT 1 FROM dbo.Parametros WHERE Clave = N'Citas.MaximoReprogramaciones')
    INSERT dbo.Parametros (Clave, Valor, TipoDato, Descripcion)
    VALUES (N'Citas.MaximoReprogramaciones', N'3', N'Int', N'Máximo de reprogramaciones permitidas por cita.');

IF NOT EXISTS (SELECT 1 FROM dbo.Parametros WHERE Clave = N'Citas.DuracionPredeterminadaMinutos')
    INSERT dbo.Parametros (Clave, Valor, TipoDato, Descripcion)
    VALUES (N'Citas.DuracionPredeterminadaMinutos', N'30', N'Int', N'Duración predeterminada de una cita en minutos.');

/* =========================== 11. VERIFICACION RAPIDA ====================== */

SELECT N'Roles' AS Entidad, COUNT(*) AS Registros FROM dbo.Roles
UNION ALL SELECT N'Especialidades', COUNT(*) FROM dbo.Especialidades
UNION ALL SELECT N'Usuarios', COUNT(*) FROM dbo.Usuarios
UNION ALL SELECT N'Parametros', COUNT(*) FROM dbo.Parametros;
