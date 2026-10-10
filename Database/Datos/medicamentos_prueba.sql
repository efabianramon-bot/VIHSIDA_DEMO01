-- Medicamentos antirretrovirales de prueba, para poder emitir recetas
-- TARV (RF-CORE-03) y luego despacharlas (RF-CORE-04).
-- SOLO para desarrollo y la demo. Se puede ejecutar mas de una vez.

SET NOCOUNT ON;

INSERT INTO dbo.Medicamentos (Nombre, Presentacion, UnidadMedida)
SELECT v.Nombre, v.Presentacion, v.UnidadMedida
FROM (VALUES
    ('Tenofovir/Lamivudina/Dolutegravir (TLD)', '300/300/50 mg', 'Tableta'),
    ('Abacavir/Lamivudina',                     '600/300 mg',    'Tableta'),
    ('Efavirenz',                               '600 mg',        'Tableta')
) AS v (Nombre, Presentacion, UnidadMedida)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Medicamentos m
                  WHERE m.Nombre = v.Nombre AND m.Presentacion = v.Presentacion);
GO
