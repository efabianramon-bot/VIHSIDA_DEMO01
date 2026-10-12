-- Segundo y tercer paciente de prueba, para poder probar la apertura de
-- un expediente (RF-CORE-02), y contraseña de prueba para el login del
-- Portal Web. SOLO para desarrollo y la demo.
-- Requiere haber ejecutado antes usuarios_prueba.sql.
-- Se puede ejecutar mas de una vez.

SET NOCOUNT ON;

INSERT INTO dbo.Pacientes (Correo, PasswordHash, PasswordSalt, NombreCompleto, Cedula, FechaNacimiento)
SELECT 'paciente2.prueba@conavihsida.test', 'SIN-ACCESO', '', 'Paciente Dos De Prueba',
       '000-0000002-0', '1990-01-01'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Pacientes p
                  WHERE p.Correo = 'paciente2.prueba@conavihsida.test');

-- Tercer paciente, que se deja SIN expediente a proposito: sirve para
-- mostrar en el Portal Medico la apertura de un expediente nuevo.
INSERT INTO dbo.Pacientes (Correo, PasswordHash, PasswordSalt, NombreCompleto, Cedula, FechaNacimiento)
SELECT 'paciente3.prueba@conavihsida.test', 'SIN-ACCESO', '', 'Paciente Tres De Prueba',
       '000-0000003-0', '1985-06-15'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Pacientes p
                  WHERE p.Correo = 'paciente3.prueba@conavihsida.test');

-- Login del Portal Web: los pacientes de prueba 1 y 2 pueden entrar con la
-- MISMA contraseña que los usuarios de usuarios_prueba.sql (se copia su
-- hash, para no repetirlo aqui). Solo se cambia si su hash todavia no es
-- uno real de BCrypt (empieza por "$2"). Los demas pacientes de prueba
-- quedan sin acceso a proposito.
DECLARE @hash VARCHAR(255) = (SELECT PasswordHash FROM dbo.Usuarios WHERE Username = 'admin.prueba');

IF @hash IS NULL
BEGIN
    RAISERROR('Ejecuta primero usuarios_prueba.sql.', 16, 1);
    RETURN;
END

UPDATE dbo.Pacientes
SET PasswordHash = @hash
WHERE Correo IN ('paciente.prueba@conavihsida.test', 'paciente2.prueba@conavihsida.test')
  AND PasswordHash NOT LIKE '$2%';
GO
