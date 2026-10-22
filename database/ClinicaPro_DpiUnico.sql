IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UQ_Pacientes_Documento'
      AND object_id = OBJECT_ID(N'dbo.Pacientes'))
BEGIN
    CREATE UNIQUE INDEX UQ_Pacientes_Documento
    ON dbo.Pacientes (Documento)
    WHERE Documento IS NOT NULL AND Documento <> N'';
END;
