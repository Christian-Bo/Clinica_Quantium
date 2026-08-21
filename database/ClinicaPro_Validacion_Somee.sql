/* ============================================================================
   CLINICA PRO - VALIDACION PARA SOMEE
   Ejecutar conectado directamente a la base de Clinica Pro creada en Somee.
   Este script no modifica datos.
   ============================================================================ */

SET NOCOUNT ON;

PRINT '=== 1. BASE Y VERSION ===';
SELECT
    DB_NAME() AS BaseActual,
    SERVERPROPERTY('ProductVersion') AS VersionSQL,
    SERVERPROPERTY('Edition') AS Edicion;

PRINT '=== 2. TABLAS ESPERADAS ===';
DECLARE @TablasEsperadas TABLE (Nombre SYSNAME PRIMARY KEY);
INSERT INTO @TablasEsperadas (Nombre)
VALUES
(N'Roles'), (N'Usuarios'), (N'UsuarioRoles'), (N'RolClaims'),
(N'UsuarioClaims'), (N'UsuarioLogins'), (N'UsuarioTokens'),
(N'Pacientes'), (N'Medicos'), (N'Especialidades'),
(N'MedicoEspecialidad'), (N'Horarios'), (N'Citas'),
(N'HistorialCitas'), (N'Notificaciones'), (N'IntentosNotificacion'),
(N'Auditoria'), (N'Parametros');

SELECT
    e.Nombre AS TablaEsperada,
    CASE WHEN t.object_id IS NULL THEN N'FALTA' ELSE N'OK' END AS Estado
FROM @TablasEsperadas e
LEFT JOIN sys.tables t
    ON t.name = e.Nombre
   AND SCHEMA_NAME(t.schema_id) = N'dbo'
ORDER BY e.Nombre;

PRINT '=== 3. ROLES ===';
SELECT Name, NormalizedName, IsActive
FROM dbo.Roles
ORDER BY Name;

PRINT '=== 4. PARAMETROS ===';
SELECT Clave, Valor, TipoDato, IsActive
FROM dbo.Parametros
ORDER BY Clave;

PRINT '=== 5. FOREIGN KEYS ===';
SELECT
    fk.name AS ForeignKey,
    OBJECT_NAME(fk.parent_object_id) AS TablaOrigen,
    OBJECT_NAME(fk.referenced_object_id) AS TablaDestino,
    fk.is_disabled AS Deshabilitada,
    fk.is_not_trusted AS NoConfiable,
    fk.delete_referential_action_desc AS AccionDelete
FROM sys.foreign_keys fk
ORDER BY TablaOrigen, ForeignKey;

PRINT '=== 6. CHECK CONSTRAINTS ===';
SELECT
    OBJECT_NAME(cc.parent_object_id) AS Tabla,
    cc.name AS Restriccion,
    cc.is_disabled AS Deshabilitada,
    cc.is_not_trusted AS NoConfiable
FROM sys.check_constraints cc
ORDER BY Tabla, Restriccion;

PRINT '=== 7. INDICES CRITICOS ===';
DECLARE @IndicesEsperados TABLE (Nombre SYSNAME PRIMARY KEY);
INSERT INTO @IndicesEsperados (Nombre)
VALUES
(N'IX_Citas_MedicoId_Fecha'),
(N'IX_Citas_Estado'),
(N'IX_Citas_PacienteId_Fecha'),
(N'UX_Citas_Medico_HorarioActivo'),
(N'IX_HistorialCitas_CitaId'),
(N'IX_Notificaciones_Estado'),
(N'UX_MedicoEspecialidad_PrimarioActivo'),
(N'IX_Horarios_MedicoId_DiaSemana');

SELECT
    e.Nombre AS IndiceEsperado,
    CASE WHEN i.index_id IS NULL THEN N'FALTA' ELSE N'OK' END AS Estado,
    OBJECT_NAME(i.object_id) AS Tabla,
    i.is_unique AS EsUnico,
    i.has_filter AS EsFiltrado,
    i.filter_definition AS Filtro
FROM @IndicesEsperados e
LEFT JOIN sys.indexes i
    ON i.name = e.Nombre
ORDER BY e.Nombre;

PRINT '=== 8. TRIGGERS DE CITAS ===';
SELECT
    tr.name AS TriggerNombre,
    OBJECT_NAME(tr.parent_id) AS Tabla,
    tr.is_disabled AS Deshabilitado
FROM sys.triggers tr
WHERE tr.parent_id = OBJECT_ID(N'dbo.Citas')
ORDER BY tr.name;

PRINT '=== 9. CONSTRAINTS SOBRE DATOS ===';
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;

PRINT '=== 10. SOLAPAMIENTOS EXISTENTES ===';
SELECT
    c1.CitaId AS Cita1,
    c2.CitaId AS Cita2,
    c1.MedicoId,
    c1.FechaHoraInicio AS Inicio1,
    c1.FechaHoraFin AS Fin1,
    c2.FechaHoraInicio AS Inicio2,
    c2.FechaHoraFin AS Fin2
FROM dbo.Citas c1
JOIN dbo.Citas c2
    ON c1.MedicoId = c2.MedicoId
   AND c1.CitaId < c2.CitaId
   AND c1.BloqueaHorario = 1
   AND c2.BloqueaHorario = 1
   AND c1.FechaHoraInicio < c2.FechaHoraFin
   AND c2.FechaHoraInicio < c1.FechaHoraFin;

PRINT '=== 11. MEDICOS PRIMARIOS DUPLICADOS ===';
SELECT
    EspecialidadId,
    COUNT(*) AS PrimariosActivos
FROM dbo.MedicoEspecialidad
WHERE EsPrimario = 1
  AND IsActive = 1
GROUP BY EspecialidadId
HAVING COUNT(*) > 1;

PRINT '=== VALIDACION TERMINADA ===';
PRINT 'Esperado: todas las tablas/indices en OK, triggers habilitados,';
PRINT 'sin violaciones de constraints, sin solapamientos y sin primarios duplicados.';
