-- Pacientes de prueba para las pruebas conjuntas con Caja, que identifica
-- al paciente por su cedula. SOLO para desarrollo y la demo.
-- Se puede ejecutar mas de una vez.
--
-- Requiere haber ejecutado antes medico_prueba.sql y medicamentos_prueba.sql.
--
--   Cedula          Caso                                   Subsidio esperado
--   000-0000004-0   Expediente Activo y una receta Activa  Si, 100 %
--   000-0000005-0   Expediente Inactivo                    No
--   000-0000006-0   Paciente sin expediente                No
--   999-9999999-9   No existe (no se inserta nada)         Paciente inexistente
--
-- Ninguno puede iniciar sesion: su PasswordHash no es un hash valido.

SET NOCOUNT ON;

DECLARE @usuarioMedico INT = (SELECT UsuarioID FROM dbo.Usuarios WHERE Username = 'medico2.prueba');
DECLARE @medico INT = (SELECT MedicoID FROM dbo.Medicos WHERE UsuarioID = @usuarioMedico);
DECLARE @medicamento INT = (SELECT TOP 1 MedicamentoID FROM dbo.Medicamentos
                            WHERE Nombre LIKE 'Tenofovir/Lamivudina/Dolutegravir%' ORDER BY MedicamentoID);

IF @medico IS NULL OR @medicamento IS NULL
BEGIN
    RAISERROR('Ejecuta primero medico_prueba.sql y medicamentos_prueba.sql.', 16, 1);
    RETURN;
END

INSERT INTO dbo.Pacientes (Correo, PasswordHash, PasswordSalt, NombreCompleto, Cedula, FechaNacimiento)
SELECT v.Correo, 'SIN-ACCESO', '', v.Nombre, v.Cedula, v.Nacimiento
FROM (VALUES
    ('caja.activo.prueba@conavihsida.test',         'Caja Activo De Prueba',         '000-0000004-0', '1988-03-10'),
    ('caja.inactivo.prueba@conavihsida.test',       'Caja Inactivo De Prueba',       '000-0000005-0', '1979-11-22'),
    ('caja.sinexpediente.prueba@conavihsida.test',  'Caja Sin Expediente De Prueba', '000-0000006-0', '1995-07-04')
) AS v (Correo, Nombre, Cedula, Nacimiento)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Pacientes p WHERE p.Cedula = v.Cedula);

-- Los codigos son fijos solo porque son datos de prueba: los reales se
-- generan al azar al abrir el expediente.
INSERT INTO dbo.ExpedientesClinicos (CodigoSerologico, PacienteID, MedicoResponsableID, FechaApertura, Estado)
SELECT v.Codigo, p.PacienteID, @medico, GETDATE(), v.Estado
FROM (VALUES
    ('COD-CAJA-0001', '000-0000004-0', 'Activo'),
    ('COD-CAJA-0002', '000-0000005-0', 'Inactivo')
) AS v (Codigo, Cedula, Estado)
JOIN dbo.Pacientes p ON p.Cedula = v.Cedula
WHERE NOT EXISTS (SELECT 1 FROM dbo.ExpedientesClinicos e WHERE e.PacienteID = p.PacienteID);

-- Una receta Activa para el paciente con subsidio. Las recetas vencen a
-- los 30 dias: si ya no le queda ninguna Activa, se emite otra, asi que
-- basta volver a ejecutar este script antes de una prueba.
IF EXISTS (SELECT 1 FROM dbo.ExpedientesClinicos WHERE CodigoSerologico = 'COD-CAJA-0001')
   AND NOT EXISTS (SELECT 1 FROM dbo.Recetas
                   WHERE CodigoSerologico = 'COD-CAJA-0001' AND Estado = 'Activa'
                     AND FechaEmision >= DATEADD(DAY, -25, GETDATE()))
BEGIN
    DECLARE @historial INT;

    INSERT INTO dbo.HistorialClinico (CodigoSerologico, UsuarioMedicoID, FechaConsulta, NotasClinicas)
    VALUES ('COD-CAJA-0001', @usuarioMedico, GETDATE(), 'Consulta de prueba para Caja');
    SET @historial = SCOPE_IDENTITY();

    INSERT INTO dbo.Recetas (HistorialID, CodigoSerologico, MedicamentoID, EsquemaTARV, CantidadPrescrita, FechaEmision, Estado)
    VALUES (@historial, 'COD-CAJA-0001', @medicamento, '1 tableta diaria', 30, GETDATE(), 'Activa');
END

SELECT p.Cedula, p.NombreCompleto, ISNULL(e.Estado, 'Sin expediente') AS Expediente,
       (SELECT MAX(r.RecetaID) FROM dbo.Recetas r
        WHERE r.CodigoSerologico = e.CodigoSerologico AND r.Estado = 'Activa') AS RecetaActiva
FROM dbo.Pacientes p
LEFT JOIN dbo.ExpedientesClinicos e ON e.PacienteID = p.PacienteID
WHERE p.Cedula IN ('000-0000004-0', '000-0000005-0', '000-0000006-0')
ORDER BY p.Cedula;
GO
