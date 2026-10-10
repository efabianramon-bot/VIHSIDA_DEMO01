-- Segundo paciente de prueba, para poder probar la apertura de un
-- expediente (RF-CORE-02). SOLO para desarrollo y la demo.
-- No puede iniciar sesion: su PasswordHash no es un hash valido.
-- Se puede ejecutar mas de una vez.

SET NOCOUNT ON;

INSERT INTO dbo.Pacientes (Correo, PasswordHash, PasswordSalt, NombreCompleto, Cedula, FechaNacimiento)
SELECT 'paciente2.prueba@conavihsida.test', 'SIN-ACCESO', '', 'Paciente Dos De Prueba',
       '000-0000002-0', '1990-01-01'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Pacientes p
                  WHERE p.Correo = 'paciente2.prueba@conavihsida.test');
GO
