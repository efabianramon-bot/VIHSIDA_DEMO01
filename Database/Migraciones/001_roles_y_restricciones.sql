-- Migracion 001: roles del SRS v1.2 y restricciones faltantes (RF-CORE-01 / RF-CORE-05).
-- Se puede ejecutar mas de una vez: cada cambio revisa antes si ya existe.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET XACT_ABORT ON
GO

BEGIN TRANSACTION;

-- RF-CORE-01: los cinco roles del RBAC. Los nombres deben coincidir con
-- NombresRol (Conavihsida.Core.Domain.Catalogos).
INSERT INTO dbo.Roles (NombreRol, Descripcion)
SELECT v.NombreRol, v.Descripcion
FROM (VALUES
    ('Administrador', 'Administracion del sistema y dashboard ejecutivo'),
    ('Medico',        'Acceso a expedientes, historial clinico y recetas'),
    ('Farmaceutico',  'Despacho de recetas e inventario'),
    ('Auditor',       'Consulta de la bitacora de auditoria'),
    ('Cajero',        'Cobros y comprobantes; sin acceso a datos clinicos')
) AS v (NombreRol, Descripcion)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.NombreRol = v.NombreRol);

IF OBJECT_ID('dbo.CK_HistorialClinico_CargaViral', 'C') IS NULL
    ALTER TABLE dbo.HistorialClinico WITH CHECK
        ADD CONSTRAINT CK_HistorialClinico_CargaViral
        CHECK (CargaViral IS NULL OR CargaViral >= 0);

-- Hora en formato HH:mm de 24 horas (00:00 a 23:59).
IF OBJECT_ID('dbo.CK_Citas_Hora', 'C') IS NULL
    ALTER TABLE dbo.Citas WITH CHECK
        ADD CONSTRAINT CK_Citas_Hora
        CHECK (Hora LIKE '[0-1][0-9]:[0-5][0-9]' OR Hora LIKE '2[0-3]:[0-5][0-9]');

IF OBJECT_ID('dbo.CK_ResultadosLab_Listo_Requiere_Fecha', 'C') IS NULL
    ALTER TABLE dbo.ResultadosLab WITH CHECK
        ADD CONSTRAINT CK_ResultadosLab_Listo_Requiere_Fecha
        CHECK (Estado <> 'Listo' OR FechaResultado IS NOT NULL);

IF OBJECT_ID('dbo.UQ_Medicamentos_Nombre', 'UQ') IS NULL
    ALTER TABLE dbo.Medicamentos
        ADD CONSTRAINT UQ_Medicamentos_Nombre UNIQUE (Nombre, Presentacion);

-- CORE = aplicacion de empleados (RF-CORE-06 / 07 / 08).
IF OBJECT_ID('dbo.CK_SesionesLog_OrigenAcceso', 'C') IS NULL
    ALTER TABLE dbo.SesionesLog WITH CHECK
        ADD CONSTRAINT CK_SesionesLog_OrigenAcceso
        CHECK (OrigenAcceso IN ('WEB', 'CAJA', 'CORE'));

COMMIT TRANSACTION;
GO
