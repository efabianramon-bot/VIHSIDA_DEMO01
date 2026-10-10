-- Dos facturas de entrega ARV subsidiada (RN-02: monto RD$ 0.00) para el
-- paciente de prueba, para que el indicador de subsidios del dashboard
-- (RF-CORE-08) tenga datos. SOLO para desarrollo y la demo.
-- Se puede ejecutar mas de una vez.

SET NOCOUNT ON;

INSERT INTO dbo.Facturas (PacienteID, Concepto, Monto, EsSubsidiada)
SELECT p.PacienteID, v.Concepto, 0, 1
FROM (VALUES
    ('Entrega ARV subsidiada - prueba 1'),
    ('Entrega ARV subsidiada - prueba 2')
) AS v (Concepto)
CROSS JOIN (SELECT MIN(PacienteID) AS PacienteID FROM dbo.Pacientes) AS p
WHERE p.PacienteID IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.Facturas f WHERE f.Concepto = v.Concepto);
GO
