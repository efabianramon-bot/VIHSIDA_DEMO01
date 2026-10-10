-- Segundo medico de prueba, con el que SI se puede iniciar sesion.
-- El usuario original medico.prueba tiene un hash de relleno
-- ("hash-temporal-no-usar-para-login") y no puede entrar.
-- SOLO para desarrollo y la demo. Se puede ejecutar mas de una vez.
--
-- Requiere haber ejecutado antes usuarios_prueba.sql: medico2.prueba
-- usa la MISMA contraseña que los usuarios de ese script (se copia su
-- hash, para no repetirlo aqui).

SET NOCOUNT ON;

DECLARE @hash VARCHAR(255) = (SELECT PasswordHash FROM dbo.Usuarios WHERE Username = 'admin.prueba');

IF @hash IS NULL
BEGIN
    RAISERROR('Ejecuta primero usuarios_prueba.sql.', 16, 1);
    RETURN;
END

INSERT INTO dbo.Usuarios (Username, Email, Nombres, Apellidos, PasswordHash)
SELECT 'medico2.prueba', 'medico2.prueba@conavihsida.test', 'Medica', 'Dos De Prueba', @hash
WHERE NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Username = 'medico2.prueba');

INSERT INTO dbo.UsuarioRoles (UsuarioID, RolID)
SELECT u.UsuarioID, r.RolID
FROM dbo.Usuarios u
CROSS JOIN dbo.Roles r
WHERE u.Username = 'medico2.prueba' AND r.NombreRol = 'Medico'
  AND NOT EXISTS (SELECT 1 FROM dbo.UsuarioRoles ur
                  WHERE ur.UsuarioID = u.UsuarioID AND ur.RolID = r.RolID);

INSERT INTO dbo.Medicos (UsuarioID, Especialidad)
SELECT u.UsuarioID, 'Infectologia'
FROM dbo.Usuarios u
WHERE u.Username = 'medico2.prueba'
  AND NOT EXISTS (SELECT 1 FROM dbo.Medicos m WHERE m.UsuarioID = u.UsuarioID);
GO
