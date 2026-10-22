IF COL_LENGTH(N'dbo.Pacientes', N'Sexo') IS NULL
    ALTER TABLE dbo.Pacientes ADD Sexo NVARCHAR(1) NULL;

IF COL_LENGTH(N'dbo.Pacientes', N'Alergias') IS NULL
    ALTER TABLE dbo.Pacientes ADD Alergias NVARCHAR(500) NULL;

IF COL_LENGTH(N'dbo.Pacientes', N'ContactoEmergenciaNombre') IS NULL
    ALTER TABLE dbo.Pacientes ADD ContactoEmergenciaNombre NVARCHAR(150) NULL;

IF COL_LENGTH(N'dbo.Pacientes', N'ContactoEmergenciaTelefono') IS NULL
    ALTER TABLE dbo.Pacientes ADD ContactoEmergenciaTelefono NVARCHAR(30) NULL;
