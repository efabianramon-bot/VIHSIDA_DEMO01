-- Migracion 005: filiales y la filial de cada usuario, para la
-- administracion de usuarios (un empleado puede estar asociado a una
-- filial o no tener ninguna).
-- Se puede ejecutar mas de una vez: cada cambio revisa antes si ya existe.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET XACT_ABORT ON
GO

BEGIN TRANSACTION;

IF OBJECT_ID('dbo.Filiales', 'U') IS NULL
    CREATE TABLE dbo.Filiales (
        FilialID  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Filiales PRIMARY KEY,
        Nombre    VARCHAR(100) NOT NULL CONSTRAINT UQ_Filiales_Nombre UNIQUE,
        Activa    BIT NOT NULL CONSTRAINT DF_Filiales_Activa DEFAULT (1)
    );

-- NULL = usuario sin filial asignada.
IF COL_LENGTH('dbo.Usuarios', 'FilialID') IS NULL
    ALTER TABLE dbo.Usuarios
        ADD FilialID INT NULL
            CONSTRAINT FK_Usuarios_Filiales REFERENCES dbo.Filiales (FilialID);

COMMIT TRANSACTION;
GO
