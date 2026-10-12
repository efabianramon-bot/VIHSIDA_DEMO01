-- Migracion 003: columnas para frenar los intentos repetidos de adivinar
-- la contraseña de un paciente (login del Portal Web).
-- Se puede ejecutar mas de una vez: cada cambio revisa antes si ya existe.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET XACT_ABORT ON
GO

BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Pacientes', 'IntentosFallidos') IS NULL
    ALTER TABLE dbo.Pacientes
        ADD IntentosFallidos INT NOT NULL
            CONSTRAINT DF_Pacientes_IntentosFallidos DEFAULT (0);

-- NULL = sin bloqueo. El bloqueo es temporal y se levanta solo: no hay
-- pantalla de administracion de pacientes para desbloquear a mano.
IF COL_LENGTH('dbo.Pacientes', 'BloqueadoHasta') IS NULL
    ALTER TABLE dbo.Pacientes ADD BloqueadoHasta DATETIME NULL;

COMMIT TRANSACTION;
GO
