-- Migracion 008: cada cobro Completado que Caja sincroniza crea la factura
-- del paciente (ya Pagada, con su pago y su comprobante), para que la vea
-- en el Portal Web. Si Caja anula el cobro, la factura pasa a Anulada.
--  - Facturas.TransaccionCajaID: la transaccion de Caja que la origino.
--    El indice unico filtrado impide crear dos facturas para el mismo
--    cobro aunque Caja lo reenvie.
--  - Facturas.Estado admite 'Anulada'.
-- Se puede ejecutar mas de una vez: cada cambio revisa antes si ya existe.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET XACT_ABORT ON
GO

BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Facturas', 'TransaccionCajaID') IS NULL
    ALTER TABLE dbo.Facturas ADD TransaccionCajaID INT NULL
        CONSTRAINT FK_Facturas_CajaTransacciones REFERENCES dbo.CajaTransacciones (TransaccionID);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE parent_object_id = OBJECT_ID('dbo.Facturas') AND name = 'CK_Facturas_Estado')
BEGIN
    -- La restriccion original del estado tiene un nombre generado por SQL
    -- Server; se busca por su definicion para quitarla.
    DECLARE @restriccion SYSNAME = (
        SELECT TOP 1 name
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID('dbo.Facturas')
          AND definition LIKE '%Estado%' AND definition LIKE '%Pagada%');

    IF @restriccion IS NOT NULL
    BEGIN
        DECLARE @sql NVARCHAR(400) = N'ALTER TABLE dbo.Facturas DROP CONSTRAINT ' + QUOTENAME(@restriccion) + N';';
        EXEC sp_executesql @sql;
    END

    ALTER TABLE dbo.Facturas ADD CONSTRAINT CK_Facturas_Estado
        CHECK (Estado IN ('Pendiente', 'Pagada', 'Anulada'));
END

COMMIT TRANSACTION;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.Facturas') AND name = 'UX_Facturas_TransaccionCaja')
    CREATE UNIQUE INDEX UX_Facturas_TransaccionCaja ON dbo.Facturas (TransaccionCajaID)
        WHERE TransaccionCajaID IS NOT NULL;
GO
