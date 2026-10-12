-- Migracion 002: la cedula del paciente pasa a ser obligatoria, unica y
-- con un solo formato (000-0000000-0). Acordado con Caja: sus
-- transacciones identifican al paciente por la cedula.
-- Se puede ejecutar mas de una vez: cada cambio revisa antes si ya existe.
--
-- Antes, la columna admitia NULL y era UNIQUE, y SQL Server solo deja un
-- NULL en una columna UNIQUE: no cabia mas de un paciente sin cedula.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET XACT_ABORT ON
GO

BEGIN TRANSACTION;

-- El paciente de prueba original (expediente COD-TEST-0001) venia sin
-- cedula. Se le da una de prueba para que la migracion pueda aplicarse.
UPDATE p
SET p.Cedula = '000-0000001-0'
FROM dbo.Pacientes p
JOIN dbo.ExpedientesClinicos e ON e.PacienteID = p.PacienteID
WHERE e.CodigoSerologico = 'COD-TEST-0001' AND p.Cedula IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.Pacientes x WHERE x.Cedula = '000-0000001-0');

-- Las cedulas escritas solo con digitos se pasan al formato con guiones.
UPDATE dbo.Pacientes
SET Cedula = STUFF(STUFF(Cedula, 4, 0, '-'), 12, 0, '-')
WHERE Cedula LIKE REPLICATE('[0-9]', 11);

IF EXISTS (SELECT 1 FROM dbo.Pacientes
           WHERE Cedula IS NULL
              OR Cedula NOT LIKE '[0-9][0-9][0-9]-[0-9][0-9][0-9][0-9][0-9][0-9][0-9]-[0-9]')
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR('Hay pacientes sin cedula o con una cedula que no tiene el formato 000-0000000-0. Corrigelos y vuelve a ejecutar la migracion.', 16, 1);
    RETURN;
END

IF COLUMNPROPERTY(OBJECT_ID('dbo.Pacientes'), 'Cedula', 'AllowsNull') = 1
BEGIN
    -- La restriccion UNIQUE original tiene un nombre generado por SQL
    -- Server; hay que quitarla para poder cambiar la columna.
    DECLARE @restriccion SYSNAME = (
        SELECT TOP 1 kc.name
        FROM sys.key_constraints kc
        JOIN sys.index_columns ic ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE kc.parent_object_id = OBJECT_ID('dbo.Pacientes') AND kc.type = 'UQ' AND c.name = 'Cedula');

    IF @restriccion IS NOT NULL
    BEGIN
        DECLARE @sql NVARCHAR(400) = N'ALTER TABLE dbo.Pacientes DROP CONSTRAINT ' + QUOTENAME(@restriccion) + N';';
        EXEC sp_executesql @sql;
    END

    ALTER TABLE dbo.Pacientes ALTER COLUMN Cedula VARCHAR(20) NOT NULL;
END

IF NOT EXISTS (SELECT 1
               FROM sys.key_constraints kc
               JOIN sys.index_columns ic ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
               JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
               WHERE kc.parent_object_id = OBJECT_ID('dbo.Pacientes') AND kc.type = 'UQ' AND c.name = 'Cedula')
    ALTER TABLE dbo.Pacientes ADD CONSTRAINT UQ_Pacientes_Cedula UNIQUE (Cedula);

-- Un solo formato: asi la restriccion UNIQUE no deja pasar la misma
-- cedula escrita de dos maneras.
IF OBJECT_ID('dbo.CK_Pacientes_Cedula_Formato', 'C') IS NULL
    ALTER TABLE dbo.Pacientes WITH CHECK
        ADD CONSTRAINT CK_Pacientes_Cedula_Formato
        CHECK (Cedula LIKE '[0-9][0-9][0-9]-[0-9][0-9][0-9][0-9][0-9][0-9][0-9]-[0-9]');

COMMIT TRANSACTION;
GO
