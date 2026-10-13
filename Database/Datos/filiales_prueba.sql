-- Filiales de prueba, para poder asociar usuarios a una filial desde la
-- administracion de usuarios. SOLO para desarrollo y la demo: los nombres
-- son de ejemplo, sustituyelos por los reales.
-- Requiere la migracion 005. Se puede ejecutar mas de una vez.

SET NOCOUNT ON;

INSERT INTO dbo.Filiales (Nombre)
SELECT v.Nombre
FROM (VALUES
    ('Sede Central'),
    ('Filial Norte'),
    ('Filial Este')
) AS v (Nombre)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Filiales f WHERE f.Nombre = v.Nombre);
GO
