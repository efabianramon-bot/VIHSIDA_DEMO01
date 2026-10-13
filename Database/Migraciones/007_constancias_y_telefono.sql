-- Migracion 007: constancias de lo que el paciente hace en el Portal Web
-- (crear la cuenta, agendar o cancelar una cita, pagar una factura) y
-- telefono del paciente. El Portal Web no tiene base de datos propia: las
-- constancias se guardan aqui para que no se pierdan al reiniciarlo.
-- Se puede ejecutar mas de una vez: cada cambio revisa antes si ya existe.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET XACT_ABORT ON
GO

BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Pacientes', 'Telefono') IS NULL
    ALTER TABLE dbo.Pacientes ADD Telefono VARCHAR(20) NULL;

-- Numero es calculado: CON-AAAAMMDD-NNNN, con el Id de la fila, asi que
-- nunca se repite ni depende de un contador aparte.
-- Ni la constancia ni sus detalles deben llevar informacion clinica (RN-01).
IF OBJECT_ID('dbo.ConstanciasPaciente', 'U') IS NULL
    CREATE TABLE dbo.ConstanciasPaciente (
        ConstanciaID  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ConstanciasPaciente PRIMARY KEY,
        PacienteID    INT           NOT NULL
            CONSTRAINT FK_ConstanciasPaciente_Pacientes REFERENCES dbo.Pacientes (PacienteID),
        Tipo          VARCHAR(40)   NOT NULL,
        Titulo        VARCHAR(150)  NOT NULL,
        Descripcion   VARCHAR(300)  NULL,
        FechaEmision  DATETIME      NOT NULL,
        ReferenciaID  INT           NULL,
        Numero AS ('CON-' + CONVERT(CHAR(8), FechaEmision, 112) + '-'
                   + CASE WHEN ConstanciaID < 10000
                          THEN RIGHT('000' + CAST(ConstanciaID AS VARCHAR(10)), 4)
                          ELSE CAST(ConstanciaID AS VARCHAR(10)) END)
    );

-- Las lineas "etiqueta: valor" de cada constancia, en el orden en que se
-- muestran (por ejemplo "Medico: Dra. Ramirez", "Hora: 10:00").
IF OBJECT_ID('dbo.ConstanciaDetalles', 'U') IS NULL
    CREATE TABLE dbo.ConstanciaDetalles (
        ConstanciaID  INT          NOT NULL
            CONSTRAINT FK_ConstanciaDetalles_Constancias REFERENCES dbo.ConstanciasPaciente (ConstanciaID),
        Orden         INT          NOT NULL,
        Etiqueta      VARCHAR(60)  NOT NULL,
        Valor         VARCHAR(200) NOT NULL,
        CONSTRAINT PK_ConstanciaDetalles PRIMARY KEY (ConstanciaID, Orden)
    );

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.ConstanciasPaciente') AND name = 'IX_ConstanciasPaciente_Paciente')
    CREATE INDEX IX_ConstanciasPaciente_Paciente ON dbo.ConstanciasPaciente (PacienteID, FechaEmision DESC);

COMMIT TRANSACTION;
GO
