IF DB_ID(N'ClinicaPro') IS NULL
BEGIN
    CREATE DATABASE ClinicaPro;
END;
GO

USE ClinicaPro;
GO

IF OBJECT_ID(N'dbo.Specialties', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Specialties
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Specialties PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Specialties_IsActive DEFAULT 1,
        CONSTRAINT UQ_Specialties_Name UNIQUE (Name)
    );
END;
GO

IF OBJECT_ID(N'dbo.Doctors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Doctors
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Doctors PRIMARY KEY,
        FullName NVARCHAR(150) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Doctors_IsActive DEFAULT 1
    );
END;
GO

IF OBJECT_ID(N'dbo.Patients', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Patients
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Patients PRIMARY KEY,
        FullName NVARCHAR(150) NOT NULL,
        Phone NVARCHAR(30) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Patients_IsActive DEFAULT 1
    );
END;
GO

IF OBJECT_ID(N'dbo.DoctorSpecialties', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DoctorSpecialties
    (
        DoctorId UNIQUEIDENTIFIER NOT NULL,
        SpecialtyId UNIQUEIDENTIFIER NOT NULL,
        IsPrimary BIT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_DoctorSpecialties_IsActive DEFAULT 1,
        CONSTRAINT PK_DoctorSpecialties PRIMARY KEY (DoctorId, SpecialtyId),
        CONSTRAINT FK_DoctorSpecialties_Doctors FOREIGN KEY (DoctorId) REFERENCES dbo.Doctors(Id),
        CONSTRAINT FK_DoctorSpecialties_Specialties FOREIGN KEY (SpecialtyId) REFERENCES dbo.Specialties(Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.DoctorSchedules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DoctorSchedules
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_DoctorSchedules PRIMARY KEY,
        DoctorId UNIQUEIDENTIFIER NOT NULL,
        DayOfWeek INT NOT NULL,
        StartTime TIME NOT NULL,
        EndTime TIME NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_DoctorSchedules_IsActive DEFAULT 1,
        CONSTRAINT CK_DoctorSchedules_Time CHECK (EndTime > StartTime),
        CONSTRAINT FK_DoctorSchedules_Doctors FOREIGN KEY (DoctorId) REFERENCES dbo.Doctors(Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.Appointments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Appointments
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Appointments PRIMARY KEY,
        PatientId UNIQUEIDENTIFIER NOT NULL,
        DoctorId UNIQUEIDENTIFIER NOT NULL,
        SpecialtyId UNIQUEIDENTIFIER NOT NULL,
        [Date] DATE NOT NULL,
        StartTime TIME NOT NULL,
        EndTime TIME NOT NULL,
        Reason NVARCHAR(200) NOT NULL,
        [Status] NVARCHAR(30) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL,
        CONSTRAINT CK_Appointments_Time CHECK (EndTime > StartTime),
        CONSTRAINT FK_Appointments_Patients FOREIGN KEY (PatientId) REFERENCES dbo.Patients(Id),
        CONSTRAINT FK_Appointments_Doctors FOREIGN KEY (DoctorId) REFERENCES dbo.Doctors(Id),
        CONSTRAINT FK_Appointments_Specialties FOREIGN KEY (SpecialtyId) REFERENCES dbo.Specialties(Id)
    );

    CREATE INDEX IX_Appointments_Doctor_Date_Start
        ON dbo.Appointments (DoctorId, [Date], StartTime);
END;
GO
