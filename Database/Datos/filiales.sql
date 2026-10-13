-- Filiales donde opera CONAVIHSIDA: la sede principal y los hospitales y
-- centros en los que presta servicio. Se usan para asociar a cada
-- empleado con el lugar donde trabaja (administracion de usuarios).
-- Requiere la migracion 005. Se puede ejecutar mas de una vez.

SET NOCOUNT ON;

-- Las primeras versiones de este script cargaban tres filiales de
-- ejemplo. "Sede Central" pasa a ser la sede principal; las otras dos se
-- borran si nadie las usa, o se desactivan si algun usuario las tiene.
UPDATE dbo.Filiales SET Nombre = 'Sede Principal'
WHERE Nombre = 'Sede Central'
  AND NOT EXISTS (SELECT 1 FROM dbo.Filiales WHERE Nombre = 'Sede Principal');

DELETE f FROM dbo.Filiales f
WHERE f.Nombre IN ('Filial Norte', 'Filial Este')
  AND NOT EXISTS (SELECT 1 FROM dbo.Usuarios u WHERE u.FilialID = f.FilialID);

UPDATE dbo.Filiales SET Activa = 0 WHERE Nombre IN ('Filial Norte', 'Filial Este');

INSERT INTO dbo.Filiales (Nombre)
SELECT v.Nombre
FROM (VALUES
    ('Sede Principal'),

    -- Santo Domingo y Distrito Nacional
    ('Hospital Dr. Robert Reid Cabral (Santo Domingo)'),
    ('Hospital Dr. Francisco Moscoso Puello (Santo Domingo)'),
    ('Instituto Dermatológico y Cirugía de Piel, IDCP (Santo Domingo)'),
    ('Hospital Materno Infantil San Lorenzo de Los Mina (Santo Domingo)'),
    ('Centro de Orientación e Investigación Integral, COIN (Santo Domingo)'),
    ('Unidad de AID FOR AIDS, Zona Oriental (Santo Domingo)'),

    -- Region Norte (Cibao)
    ('Hospital Regional Universitario José María Cabral y Báez (Santiago)'),
    ('Hospital Infantil Dr. Arturo Grullón (Santiago)'),
    ('Hospital Provincial Dr. Ricardo Limardo (Puerto Plata)'),
    ('Hospital Regional Dr. Luis L. Bogaert (Valverde, Mao)'),

    -- Region Este
    ('Hospital Regional Dr. Antonio Musa (San Pedro de Macorís)'),
    ('Hospital Provincial Nuestra Señora de la Altagracia (Higüey)'),
    ('Hospital Provincial Francisco A. Gonzalvo (La Romana)'),

    -- Region Sur
    ('Hospital Regional Universitario Jaime Mota (Barahona)'),
    ('Hospital Regional de San Juan de la Maguana (San Juan)'),
    ('Hospital Regional Juan Pablo Pina (San Cristóbal)')
) AS v (Nombre)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Filiales f WHERE f.Nombre = v.Nombre);
GO
