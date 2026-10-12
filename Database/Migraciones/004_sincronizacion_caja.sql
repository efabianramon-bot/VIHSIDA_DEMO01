-- Migracion 004: tablas donde el CORE guarda lo que sincroniza el modulo
-- de Caja a traves de Integracion (turnos, movimientos, transacciones,
-- comprobantes y fondos/convenios). Acordado con Caja: el CORE es la
-- fuente oficial; la base local de cada terminal es solo una cola.
-- Se puede ejecutar mas de una vez: cada tabla revisa antes si ya existe.
--
-- La clave de cada tabla es el numero de negocio que genera Caja
-- (NumeroTurno, NumeroTransaccion...), no IdLocal: ese es el Id de la
-- base SQLite de cada terminal y se repite de una terminal a otra.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET XACT_ABORT ON
GO

BEGIN TRANSACTION;

IF OBJECT_ID('dbo.CajaTurnos', 'U') IS NULL
    CREATE TABLE dbo.CajaTurnos (
        TurnoID            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CajaTurnos PRIMARY KEY,
        NumeroTurno        VARCHAR(40)   NOT NULL CONSTRAINT UQ_CajaTurnos_Numero UNIQUE,
        IdLocal            INT           NOT NULL,
        UsuarioCajero      VARCHAR(50)   NOT NULL,
        NombreCajero       VARCHAR(200)  NULL,
        Caja               VARCHAR(50)   NULL,
        FechaApertura      DATETIME      NOT NULL,
        FechaCierre        DATETIME      NULL,
        BalanceInicial     DECIMAL(12,2) NOT NULL,
        SaldoEsperado      DECIMAL(12,2) NOT NULL,
        EfectivoDeclarado  DECIMAL(12,2) NULL,
        Diferencia         DECIMAL(12,2) NULL,
        Estado             VARCHAR(20)   NOT NULL
            CONSTRAINT CK_CajaTurnos_Estado CHECK (Estado IN ('Abierto', 'Cerrado')),
        Observacion        VARCHAR(500)  NULL,
        FechaRecibido      DATETIME      NOT NULL,
        FechaActualizado   DATETIME      NULL
    );

IF OBJECT_ID('dbo.CajaMovimientos', 'U') IS NULL
    CREATE TABLE dbo.CajaMovimientos (
        MovimientoID       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CajaMovimientos PRIMARY KEY,
        NumeroMovimiento   VARCHAR(40)   NOT NULL CONSTRAINT UQ_CajaMovimientos_Numero UNIQUE,
        IdLocal            INT           NOT NULL,
        TurnoCajaIdLocal   INT           NOT NULL,
        Fecha              DATETIME      NOT NULL,
        Tipo               VARCHAR(20)   NOT NULL
            CONSTRAINT CK_CajaMovimientos_Tipo CHECK (Tipo IN ('Entrada', 'Salida', 'Cobro', 'Subsidio', 'Ajuste')),
        Monto              DECIMAL(12,2) NOT NULL,
        Concepto           VARCHAR(300)  NULL,
        Referencia         VARCHAR(40)   NULL,
        UsuarioCajero      VARCHAR(50)   NOT NULL,
        FechaRecibido      DATETIME      NOT NULL,
        FechaActualizado   DATETIME      NULL
    );

-- PacienteID queda NULL si la cedula no corresponde a ningun paciente
-- registrado. NumeroReceta enlaza la entrega subsidiada con la receta.
IF OBJECT_ID('dbo.CajaTransacciones', 'U') IS NULL
    CREATE TABLE dbo.CajaTransacciones (
        TransaccionID      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CajaTransacciones PRIMARY KEY,
        NumeroTransaccion  VARCHAR(40)   NOT NULL CONSTRAINT UQ_CajaTransacciones_Numero UNIQUE,
        IdLocal            INT           NOT NULL,
        Fecha              DATETIME      NOT NULL,
        CedulaPaciente     VARCHAR(20)   NOT NULL,
        PacienteID         INT           NULL
            CONSTRAINT FK_CajaTransacciones_Pacientes REFERENCES dbo.Pacientes (PacienteID),
        TipoServicio       VARCHAR(100)  NOT NULL,
        Monto              DECIMAL(12,2) NOT NULL
            CONSTRAINT CK_CajaTransacciones_Monto CHECK (Monto >= 0),
        FormaPago          VARCHAR(20)   NOT NULL
            CONSTRAINT CK_CajaTransacciones_FormaPago CHECK (FormaPago IN ('Efectivo', 'Tarjeta', 'Subsidiado')),
        EsSubsidiado       BIT           NOT NULL,
        Estado             VARCHAR(20)   NOT NULL
            CONSTRAINT CK_CajaTransacciones_Estado CHECK (Estado IN ('Pendiente', 'Completada', 'Anulada')),
        ComprobanteGenerado BIT          NOT NULL,
        UsuarioCajero      VARCHAR(50)   NOT NULL,
        NumeroReceta       INT           NULL
            CONSTRAINT FK_CajaTransacciones_Recetas REFERENCES dbo.Recetas (RecetaID),
        FechaRecibido      DATETIME      NOT NULL,
        FechaActualizado   DATETIME      NULL
    );

IF OBJECT_ID('dbo.CajaComprobantes', 'U') IS NULL
    CREATE TABLE dbo.CajaComprobantes (
        ComprobanteCajaID  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CajaComprobantes PRIMARY KEY,
        NumeroComprobante  VARCHAR(40)   NOT NULL CONSTRAINT UQ_CajaComprobantes_Numero UNIQUE,
        IdLocal            INT           NOT NULL,
        TransaccionIdLocal INT           NOT NULL,
        FechaGeneracion    DATETIME      NOT NULL,
        TipoDocumento      VARCHAR(30)   NOT NULL,
        EsReimpresion      BIT           NOT NULL,
        CantidadReimpresiones INT        NOT NULL,
        FechaRecibido      DATETIME      NOT NULL,
        FechaActualizado   DATETIME      NULL
    );

IF OBJECT_ID('dbo.CajaFondos', 'U') IS NULL
    CREATE TABLE dbo.CajaFondos (
        FondoID            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CajaFondos PRIMARY KEY,
        Codigo             VARCHAR(40)   NOT NULL CONSTRAINT UQ_CajaFondos_Codigo UNIQUE,
        IdLocal            INT           NOT NULL,
        Nombre             VARCHAR(200)  NOT NULL,
        TipoFuente         VARCHAR(40)   NOT NULL
            CONSTRAINT CK_CajaFondos_TipoFuente CHECK (TipoFuente IN ('PresupuestoEstatal', 'DonacionInternacional')),
        Organismo          VARCHAR(200)  NULL,
        MontoAsignado      DECIMAL(14,2) NOT NULL,
        MontoUtilizado     DECIMAL(14,2) NOT NULL,
        FechaInicio        DATETIME      NOT NULL,
        FechaFin           DATETIME      NULL,
        Activo             BIT           NOT NULL,
        FechaRecibido      DATETIME      NOT NULL,
        FechaActualizado   DATETIME      NULL
    );

COMMIT TRANSACTION;
GO
