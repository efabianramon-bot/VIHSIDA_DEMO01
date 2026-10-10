-- Usuarios de prueba, uno por cada rol que no tenia usuario (RF-CORE-01).
-- SOLO para desarrollo y la demo: no ejecutar en una base real.
-- Contraseña de los cuatro usuarios: Prueba.Core.2026
-- Se puede ejecutar mas de una vez.

SET NOCOUNT ON;

DECLARE @hash VARCHAR(255) = '$2a$11$6M4oi6eEWj6Y2xc5oF2TPubdGB5J153Cv3e5c4r.R0ynCNdyISjre';

INSERT INTO dbo.Usuarios (Username, Email, Nombres, Apellidos, PasswordHash)
SELECT v.Username, v.Email, v.Nombres, 'De Prueba', @hash
FROM (VALUES
    ('admin.prueba',    'admin.prueba@conavihsida.test',    'Administrador'),
    ('farmacia.prueba', 'farmacia.prueba@conavihsida.test', 'Farmaceutico'),
    ('auditor.prueba',  'auditor.prueba@conavihsida.test',  'Auditor'),
    ('cajero.prueba',   'cajero.prueba@conavihsida.test',   'Cajero')
) AS v (Username, Email, Nombres)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Usuarios u WHERE u.Username = v.Username);

-- En estos usuarios de prueba, Nombres coincide con el nombre del rol.
INSERT INTO dbo.UsuarioRoles (UsuarioID, RolID)
SELECT u.UsuarioID, r.RolID
FROM dbo.Usuarios u
JOIN dbo.Roles r ON r.NombreRol = u.Nombres
WHERE u.Username IN ('admin.prueba', 'farmacia.prueba', 'auditor.prueba', 'cajero.prueba')
  AND NOT EXISTS (SELECT 1 FROM dbo.UsuarioRoles ur
                  WHERE ur.UsuarioID = u.UsuarioID AND ur.RolID = r.RolID);
GO