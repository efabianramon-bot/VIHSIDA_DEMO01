-- Migracion 006: el correo del paciente pasa a ser opcional, para que un
-- medico pueda registrar a alguien que llega a consulta sin cuenta en el
-- Portal Web (y quiza sin correo). Sigue siendo unico cuando existe.
-- Se puede ejecutar mas de una vez: cada cambio revisa antes si ya existe.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET XACT_ABORT ON
GO

BEGIN TRANSACTION;

IF COLUMNPROPERTY(OBJECT_ID('dbo.Pacientes'), 'Correo', 'AllowsNull') = 0
BEGIN
    -- La restriccion UNIQUE original tiene un nombre generado por SQL
    -- Server; hay que quitarla para poder cambiar la columna.
    DECLARE @restriccion SYSNAME = (
        SELECT TOP 1 kc.name
        FROM sys.key_constraints kc
        JOIN sys.index_columns ic ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE kc.parent_object_id = OBJECT_ID('dbo.Pacientes') AND kc.type = 'UQ' AND c.name = 'Correo');

    IF @restriccion IS NOT NULL
    BEGIN
        DECLARE @sql NVARCHAR(400) = N'ALTER TABLE dbo.Pacientes DROP CONSTRAINT ' + QUOTENAME(@restriccion) + N';';
        EXEC sp_executesql @sql;
    END

    ALTER TABLE dbo.Pacientes ALTER COLUMN Correo VARCHAR(150) NULL;
END

COMMIT TRANSACTION;
GO

-- Indice filtrado: unico entre los que tienen correo, y admite muchos
-- pacientes sin correo (una restriccion UNIQUE normal solo admitiria uno).
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.Pacientes') AND name = 'UX_Pacientes_Correo')
    CREATE UNIQUE INDEX UX_Pacientes_Correo ON dbo.Pacientes (Correo) WHERE Correo IS NOT NULL;
GO
