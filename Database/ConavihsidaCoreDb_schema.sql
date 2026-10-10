-- Esquema de ConavihsidaCoreDb (LocalDB). Solo estructura, sin datos.
-- Generado con SMO el 2026-10-09.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ===== Tablas =====
CREATE TABLE [dbo].[Auditoria](
	[AuditoriaID] [int] IDENTITY(1,1) NOT NULL,
	[UsuarioID] [int] NULL,
	[PacienteID] [int] NULL,
	[Terminal] [varchar](100) NULL,
	[Accion] [varchar](100) NOT NULL,
	[EntidadAfectada] [varchar](60) NOT NULL,
	[EntidadID] [int] NULL,
	[FechaHora] [datetime] NOT NULL,
	[Detalle] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[AuditoriaID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[Auditoria] ADD  DEFAULT (getdate()) FOR [FechaHora]
GO

CREATE TABLE [dbo].[Citas](
	[CitaID] [int] IDENTITY(1,1) NOT NULL,
	[PacienteID] [int] NOT NULL,
	[MedicoID] [int] NOT NULL,
	[Fecha] [date] NOT NULL,
	[Hora] [varchar](5) NOT NULL,
	[Motivo] [varchar](300) NOT NULL,
	[Estado] [varchar](20) NOT NULL,
	[FechaCreacion] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[CitaID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_Citas_Medico_Fecha_Hora_Activa] ON [dbo].[Citas]
(
	[MedicoID] ASC,
	[Fecha] ASC,
	[Hora] ASC
)
WHERE ([Estado]<>'Cancelada')
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Citas] ADD  DEFAULT ('Confirmada') FOR [Estado]
GO
ALTER TABLE [dbo].[Citas] ADD  DEFAULT (getdate()) FOR [FechaCreacion]
GO
ALTER TABLE [dbo].[Citas]  WITH CHECK ADD CHECK  (([Estado]='Cancelada' OR [Estado]='Pendiente' OR [Estado]='Confirmada'))
GO
ALTER TABLE [dbo].[Citas]  WITH CHECK ADD  CONSTRAINT [CK_Citas_Hora] CHECK  (([Hora] like '[0-1][0-9]:[0-5][0-9]' OR [Hora] like '2[0-3]:[0-5][0-9]'))
GO
ALTER TABLE [dbo].[Citas] CHECK CONSTRAINT [CK_Citas_Hora]
GO

CREATE TABLE [dbo].[Comprobantes](
	[ComprobanteID] [int] IDENTITY(1,1) NOT NULL,
	[FacturaID] [int] NOT NULL,
	[NumeroRecibo] [varchar](20) NOT NULL,
	[NumeroFactura] [varchar](20) NOT NULL,
	[FechaEmision] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ComprobanteID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[FacturaID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[NumeroFactura] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[NumeroRecibo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Comprobantes] ADD  DEFAULT (getdate()) FOR [FechaEmision]
GO

CREATE TABLE [dbo].[ExpedientesClinicos](
	[CodigoSerologico] [varchar](20) NOT NULL,
	[PacienteID] [int] NOT NULL,
	[MedicoResponsableID] [int] NULL,
	[FechaApertura] [datetime] NOT NULL,
	[Estado] [varchar](20) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[CodigoSerologico] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[PacienteID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[ExpedientesClinicos] ADD  DEFAULT (getdate()) FOR [FechaApertura]
GO
ALTER TABLE [dbo].[ExpedientesClinicos] ADD  DEFAULT ('Activo') FOR [Estado]
GO
ALTER TABLE [dbo].[ExpedientesClinicos]  WITH CHECK ADD CHECK  (([Estado]='Transferido' OR [Estado]='Inactivo' OR [Estado]='Activo'))
GO

CREATE TABLE [dbo].[Facturas](
	[FacturaID] [int] IDENTITY(1,1) NOT NULL,
	[PacienteID] [int] NOT NULL,
	[Concepto] [varchar](200) NOT NULL,
	[Fecha] [date] NOT NULL,
	[Monto] [decimal](10, 2) NOT NULL,
	[EsSubsidiada] [bit] NOT NULL,
	[Estado] [varchar](20) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FacturaID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Facturas] ADD  DEFAULT (CONVERT([date],getdate())) FOR [Fecha]
GO
ALTER TABLE [dbo].[Facturas] ADD  DEFAULT ((0)) FOR [EsSubsidiada]
GO
ALTER TABLE [dbo].[Facturas] ADD  DEFAULT ('Pendiente') FOR [Estado]
GO
ALTER TABLE [dbo].[Facturas]  WITH CHECK ADD CHECK  (([Estado]='Pagada' OR [Estado]='Pendiente'))
GO
ALTER TABLE [dbo].[Facturas]  WITH CHECK ADD CHECK  (([Monto]>=(0)))
GO
ALTER TABLE [dbo].[Facturas]  WITH CHECK ADD  CONSTRAINT [CK_Facturas_SubsidioEnMontoCero] CHECK  (([EsSubsidiada]=(0) OR [Monto]=(0)))
GO
ALTER TABLE [dbo].[Facturas] CHECK CONSTRAINT [CK_Facturas_SubsidioEnMontoCero]
GO

CREATE TABLE [dbo].[HistorialClinico](
	[HistorialID] [int] IDENTITY(1,1) NOT NULL,
	[CodigoSerologico] [varchar](20) NOT NULL,
	[UsuarioMedicoID] [int] NOT NULL,
	[FechaConsulta] [datetime] NOT NULL,
	[CargaViral] [decimal](12, 2) NULL,
	[ConteoCD4] [int] NULL,
	[DiagnosticoDetalle] [varchar](max) NULL,
	[NotasClinicas] [varchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[HistorialID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[HistorialClinico] ADD  DEFAULT (getdate()) FOR [FechaConsulta]
GO
ALTER TABLE [dbo].[HistorialClinico]  WITH CHECK ADD CHECK  (([ConteoCD4] IS NULL OR [ConteoCD4]>=(0)))
GO
ALTER TABLE [dbo].[HistorialClinico]  WITH CHECK ADD  CONSTRAINT [CK_HistorialClinico_CargaViral] CHECK  (([CargaViral] IS NULL OR [CargaViral]>=(0)))
GO
ALTER TABLE [dbo].[HistorialClinico] CHECK CONSTRAINT [CK_HistorialClinico_CargaViral]
GO

CREATE TABLE [dbo].[LotesMedicamento](
	[LoteID] [int] IDENTITY(1,1) NOT NULL,
	[MedicamentoID] [int] NOT NULL,
	[NumeroLote] [varchar](50) NOT NULL,
	[FechaCaducidad] [date] NOT NULL,
	[ExistenciaActual] [int] NOT NULL,
	[ExistenciaMinima] [int] NOT NULL,
	[ExistenciaMaxima] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[LoteID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Lote_Medicamento] UNIQUE NONCLUSTERED 
(
	[MedicamentoID] ASC,
	[NumeroLote] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[LotesMedicamento] ADD  DEFAULT ((0)) FOR [ExistenciaMinima]
GO
ALTER TABLE [dbo].[LotesMedicamento]  WITH CHECK ADD CHECK  (([ExistenciaActual]>=(0)))
GO
ALTER TABLE [dbo].[LotesMedicamento]  WITH CHECK ADD CHECK  (([ExistenciaMinima]>=(0)))
GO
ALTER TABLE [dbo].[LotesMedicamento]  WITH CHECK ADD  CONSTRAINT [CK_LotesMedicamento_ExistenciaMaxima] CHECK  (([ExistenciaMaxima] IS NULL OR [ExistenciaMaxima]>=[ExistenciaMinima]))
GO
ALTER TABLE [dbo].[LotesMedicamento] CHECK CONSTRAINT [CK_LotesMedicamento_ExistenciaMaxima]
GO

CREATE TABLE [dbo].[Medicamentos](
	[MedicamentoID] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [varchar](150) NOT NULL,
	[Presentacion] [varchar](80) NULL,
	[UnidadMedida] [varchar](30) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MedicamentoID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Medicamentos_Nombre] UNIQUE NONCLUSTERED 
(
	[Nombre] ASC,
	[Presentacion] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Medicos](
	[MedicoID] [int] IDENTITY(1,1) NOT NULL,
	[UsuarioID] [int] NOT NULL,
	[Especialidad] [varchar](80) NOT NULL,
	[Activo] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MedicoID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[UsuarioID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Medicos] ADD  DEFAULT ((1)) FOR [Activo]
GO

CREATE TABLE [dbo].[MovimientosInventario](
	[MovimientoID] [int] IDENTITY(1,1) NOT NULL,
	[LoteID] [int] NOT NULL,
	[TipoMovimiento] [varchar](10) NOT NULL,
	[RecetaID] [int] NULL,
	[Cantidad] [int] NOT NULL,
	[UsuarioID] [int] NOT NULL,
	[FechaMovimiento] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MovimientoID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[MovimientosInventario] ADD  DEFAULT (getdate()) FOR [FechaMovimiento]
GO
ALTER TABLE [dbo].[MovimientosInventario]  WITH CHECK ADD CHECK  (([Cantidad]>(0)))
GO
ALTER TABLE [dbo].[MovimientosInventario]  WITH CHECK ADD CHECK  (([TipoMovimiento]='Salida' OR [TipoMovimiento]='Entrada'))
GO
ALTER TABLE [dbo].[MovimientosInventario]  WITH CHECK ADD  CONSTRAINT [CK_Movimiento_Salida_Requiere_Receta] CHECK  (([TipoMovimiento]='Salida' AND [RecetaID] IS NOT NULL OR [TipoMovimiento]='Entrada'))
GO
ALTER TABLE [dbo].[MovimientosInventario] CHECK CONSTRAINT [CK_Movimiento_Salida_Requiere_Receta]
GO

CREATE TABLE [dbo].[Pacientes](
	[PacienteID] [int] IDENTITY(1,1) NOT NULL,
	[Correo] [varchar](150) NOT NULL,
	[PasswordHash] [varchar](255) NOT NULL,
	[PasswordSalt] [varchar](255) NOT NULL,
	[NombreCompleto] [varchar](150) NOT NULL,
	[Cedula] [varchar](20) NULL,
	[FechaNacimiento] [date] NULL,
	[Activo] [bit] NOT NULL,
	[FechaRegistro] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[PacienteID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Correo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Cedula] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Pacientes] ADD  DEFAULT ((1)) FOR [Activo]
GO
ALTER TABLE [dbo].[Pacientes] ADD  DEFAULT (getdate()) FOR [FechaRegistro]
GO

CREATE TABLE [dbo].[Pagos](
	[PagoID] [int] IDENTITY(1,1) NOT NULL,
	[FacturaID] [int] NOT NULL,
	[Monto] [decimal](10, 2) NOT NULL,
	[FormaPago] [varchar](20) NOT NULL,
	[ReferenciaPasarela] [varchar](100) NULL,
	[CajeroID] [int] NULL,
	[FechaPago] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[PagoID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Pagos] ADD  DEFAULT (getdate()) FOR [FechaPago]
GO
ALTER TABLE [dbo].[Pagos]  WITH CHECK ADD CHECK  (([FormaPago]='Subsidio' OR [FormaPago]='Transferencia' OR [FormaPago]='Efectivo' OR [FormaPago]='Tarjeta'))
GO
ALTER TABLE [dbo].[Pagos]  WITH CHECK ADD CHECK  (([Monto]>=(0)))
GO

CREATE TABLE [dbo].[Recetas](
	[RecetaID] [int] IDENTITY(1,1) NOT NULL,
	[HistorialID] [int] NOT NULL,
	[CodigoSerologico] [varchar](20) NOT NULL,
	[MedicamentoID] [int] NOT NULL,
	[EsquemaTARV] [varchar](150) NULL,
	[CantidadPrescrita] [int] NOT NULL,
	[FechaEmision] [datetime] NOT NULL,
	[Estado] [varchar](20) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[RecetaID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Recetas] ADD  DEFAULT (getdate()) FOR [FechaEmision]
GO
ALTER TABLE [dbo].[Recetas] ADD  DEFAULT ('Activa') FOR [Estado]
GO
ALTER TABLE [dbo].[Recetas]  WITH CHECK ADD CHECK  (([CantidadPrescrita]>(0)))
GO
ALTER TABLE [dbo].[Recetas]  WITH CHECK ADD CHECK  (([Estado]='Cancelada' OR [Estado]='Vencida' OR [Estado]='Dispensada' OR [Estado]='Activa'))
GO

CREATE TABLE [dbo].[ResultadosLab](
	[ResultadoID] [int] IDENTITY(1,1) NOT NULL,
	[CodigoSerologico] [varchar](20) NOT NULL,
	[Prueba] [varchar](100) NOT NULL,
	[FechaSolicitud] [datetime] NOT NULL,
	[Estado] [varchar](20) NOT NULL,
	[ValorResultado] [varchar](max) NULL,
	[FechaResultado] [datetime] NULL,
	[NotaPaciente] [varchar](200) NULL,
PRIMARY KEY CLUSTERED 
(
	[ResultadoID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[ResultadosLab] ADD  DEFAULT (getdate()) FOR [FechaSolicitud]
GO
ALTER TABLE [dbo].[ResultadosLab] ADD  DEFAULT ('Pendiente') FOR [Estado]
GO
ALTER TABLE [dbo].[ResultadosLab]  WITH CHECK ADD CHECK  (([Estado]='Listo' OR [Estado]='Pendiente'))
GO
ALTER TABLE [dbo].[ResultadosLab]  WITH CHECK ADD  CONSTRAINT [CK_ResultadosLab_Listo_Requiere_Fecha] CHECK  (([Estado]<>'Listo' OR [FechaResultado] IS NOT NULL))
GO
ALTER TABLE [dbo].[ResultadosLab] CHECK CONSTRAINT [CK_ResultadosLab_Listo_Requiere_Fecha]
GO

CREATE TABLE [dbo].[Roles](
	[RolID] [int] IDENTITY(1,1) NOT NULL,
	[NombreRol] [varchar](50) NOT NULL,
	[Descripcion] [varchar](255) NULL,
	[Activo] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[RolID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[NombreRol] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Roles] ADD  DEFAULT ((1)) FOR [Activo]
GO

CREATE TABLE [dbo].[SesionesLog](
	[SesionID] [bigint] IDENTITY(1,1) NOT NULL,
	[UsuarioID] [int] NULL,
	[UsernameIngresado] [varchar](50) NOT NULL,
	[Exitoso] [bit] NOT NULL,
	[OrigenAcceso] [varchar](20) NOT NULL,
	[DireccionIP] [varchar](45) NULL,
	[MensajeError] [varchar](255) NULL,
	[FechaIntento] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[SesionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[SesionesLog] ADD  DEFAULT (getdate()) FOR [FechaIntento]
GO
ALTER TABLE [dbo].[SesionesLog]  WITH CHECK ADD  CONSTRAINT [CK_SesionesLog_OrigenAcceso] CHECK  (([OrigenAcceso]='CORE' OR [OrigenAcceso]='CAJA' OR [OrigenAcceso]='WEB'))
GO
ALTER TABLE [dbo].[SesionesLog] CHECK CONSTRAINT [CK_SesionesLog_OrigenAcceso]
GO

CREATE TABLE [dbo].[UsuarioRoles](
	[UsuarioID] [int] NOT NULL,
	[RolID] [int] NOT NULL,
	[FechaAsignacion] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[UsuarioID] ASC,
	[RolID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[UsuarioRoles] ADD  DEFAULT (getdate()) FOR [FechaAsignacion]
GO

CREATE TABLE [dbo].[Usuarios](
	[UsuarioID] [int] IDENTITY(1,1) NOT NULL,
	[Username] [varchar](50) NOT NULL,
	[Email] [varchar](100) NOT NULL,
	[Nombres] [varchar](100) NOT NULL,
	[Apellidos] [varchar](100) NOT NULL,
	[PasswordHash] [varchar](255) NOT NULL,
	[PasswordSalt] [varchar](255) NULL,
	[Activo] [bit] NOT NULL,
	[Bloqueado] [bit] NOT NULL,
	[IntentosFallidos] [int] NOT NULL,
	[FechaUltimoAcceso] [datetime] NULL,
	[FechaCreacion] [datetime] NOT NULL,
	[FechaModificacion] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[UsuarioID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Username] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ((1)) FOR [Activo]
GO
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ((0)) FOR [Bloqueado]
GO
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ((0)) FOR [IntentosFallidos]
GO
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT (getdate()) FOR [FechaCreacion]
GO

-- ===== Claves foraneas =====
ALTER TABLE [dbo].[Auditoria]  WITH CHECK ADD FOREIGN KEY([PacienteID])
REFERENCES [dbo].[Pacientes] ([PacienteID])
GO
ALTER TABLE [dbo].[Auditoria]  WITH CHECK ADD FOREIGN KEY([UsuarioID])
REFERENCES [dbo].[Usuarios] ([UsuarioID])
GO
ALTER TABLE [dbo].[Citas]  WITH CHECK ADD FOREIGN KEY([MedicoID])
REFERENCES [dbo].[Medicos] ([MedicoID])
GO
ALTER TABLE [dbo].[Citas]  WITH CHECK ADD FOREIGN KEY([PacienteID])
REFERENCES [dbo].[Pacientes] ([PacienteID])
GO
ALTER TABLE [dbo].[Comprobantes]  WITH CHECK ADD FOREIGN KEY([FacturaID])
REFERENCES [dbo].[Facturas] ([FacturaID])
GO
ALTER TABLE [dbo].[ExpedientesClinicos]  WITH CHECK ADD FOREIGN KEY([MedicoResponsableID])
REFERENCES [dbo].[Medicos] ([MedicoID])
GO
ALTER TABLE [dbo].[ExpedientesClinicos]  WITH CHECK ADD FOREIGN KEY([PacienteID])
REFERENCES [dbo].[Pacientes] ([PacienteID])
GO
ALTER TABLE [dbo].[Facturas]  WITH CHECK ADD FOREIGN KEY([PacienteID])
REFERENCES [dbo].[Pacientes] ([PacienteID])
GO
ALTER TABLE [dbo].[HistorialClinico]  WITH CHECK ADD FOREIGN KEY([CodigoSerologico])
REFERENCES [dbo].[ExpedientesClinicos] ([CodigoSerologico])
GO
ALTER TABLE [dbo].[HistorialClinico]  WITH CHECK ADD FOREIGN KEY([UsuarioMedicoID])
REFERENCES [dbo].[Usuarios] ([UsuarioID])
GO
ALTER TABLE [dbo].[LotesMedicamento]  WITH CHECK ADD FOREIGN KEY([MedicamentoID])
REFERENCES [dbo].[Medicamentos] ([MedicamentoID])
GO
ALTER TABLE [dbo].[Medicos]  WITH CHECK ADD FOREIGN KEY([UsuarioID])
REFERENCES [dbo].[Usuarios] ([UsuarioID])
GO
ALTER TABLE [dbo].[MovimientosInventario]  WITH CHECK ADD FOREIGN KEY([LoteID])
REFERENCES [dbo].[LotesMedicamento] ([LoteID])
GO
ALTER TABLE [dbo].[MovimientosInventario]  WITH CHECK ADD FOREIGN KEY([RecetaID])
REFERENCES [dbo].[Recetas] ([RecetaID])
GO
ALTER TABLE [dbo].[MovimientosInventario]  WITH CHECK ADD FOREIGN KEY([UsuarioID])
REFERENCES [dbo].[Usuarios] ([UsuarioID])
GO
ALTER TABLE [dbo].[Pagos]  WITH CHECK ADD FOREIGN KEY([CajeroID])
REFERENCES [dbo].[Usuarios] ([UsuarioID])
GO
ALTER TABLE [dbo].[Pagos]  WITH CHECK ADD FOREIGN KEY([FacturaID])
REFERENCES [dbo].[Facturas] ([FacturaID])
GO
ALTER TABLE [dbo].[Recetas]  WITH CHECK ADD FOREIGN KEY([CodigoSerologico])
REFERENCES [dbo].[ExpedientesClinicos] ([CodigoSerologico])
GO
ALTER TABLE [dbo].[Recetas]  WITH CHECK ADD FOREIGN KEY([HistorialID])
REFERENCES [dbo].[HistorialClinico] ([HistorialID])
GO
ALTER TABLE [dbo].[Recetas]  WITH CHECK ADD FOREIGN KEY([MedicamentoID])
REFERENCES [dbo].[Medicamentos] ([MedicamentoID])
GO
ALTER TABLE [dbo].[ResultadosLab]  WITH CHECK ADD FOREIGN KEY([CodigoSerologico])
REFERENCES [dbo].[ExpedientesClinicos] ([CodigoSerologico])
GO
ALTER TABLE [dbo].[SesionesLog]  WITH CHECK ADD  CONSTRAINT [FK_SesionesLog_Usuarios] FOREIGN KEY([UsuarioID])
REFERENCES [dbo].[Usuarios] ([UsuarioID])
GO
ALTER TABLE [dbo].[SesionesLog] CHECK CONSTRAINT [FK_SesionesLog_Usuarios]
GO
ALTER TABLE [dbo].[UsuarioRoles]  WITH CHECK ADD  CONSTRAINT [FK_UsuarioRoles_Roles] FOREIGN KEY([RolID])
REFERENCES [dbo].[Roles] ([RolID])
GO
ALTER TABLE [dbo].[UsuarioRoles] CHECK CONSTRAINT [FK_UsuarioRoles_Roles]
GO
ALTER TABLE [dbo].[UsuarioRoles]  WITH CHECK ADD  CONSTRAINT [FK_UsuarioRoles_Usuarios] FOREIGN KEY([UsuarioID])
REFERENCES [dbo].[Usuarios] ([UsuarioID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[UsuarioRoles] CHECK CONSTRAINT [FK_UsuarioRoles_Usuarios]
GO

-- ===== Vistas =====

        CREATE VIEW vw_ResultadosPaciente AS
        SELECT
            r.ResultadoID       AS Id,
            e.PacienteID,
            r.Prueba,
            r.FechaSolicitud,
            r.Estado,
            r.NotaPaciente       AS Nota
        FROM ResultadosLab r
        JOIN ExpedientesClinicos e ON e.CodigoSerologico = r.CodigoSerologico
GO

-- ===== Procedimientos =====
CREATE PROCEDURE sp_ObtenerUsuarioParaLogin
    @Username VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Datos del usuario
    SELECT 
        u.UsuarioID,
        u.Username,
        u.Email,
        u.Nombres,
        u.Apellidos,
        u.PasswordHash,
        u.PasswordSalt,
        u.Activo,
        u.Bloqueado,
        u.IntentosFallidos
    FROM Usuarios u
    WHERE u.Username = @Username;

    -- 2. Roles asignados al usuario (para los claims del JWT)
    SELECT 
        r.NombreRol
    FROM UsuarioRoles ur
    INNER JOIN Roles r ON ur.RolID = r.RolID
    INNER JOIN Usuarios u ON ur.UsuarioID = u.UsuarioID
    WHERE u.Username = @Username AND r.Activo = 1;
END;
GO

CREATE PROCEDURE sp_RegistrarIntentoLogin
    @UsuarioID INT = NULL,
    @UsernameIngresado VARCHAR(50),
    @Exitoso BIT,
    @OrigenAcceso VARCHAR(20),  -- 'WEB' o 'CAJA'
    @DireccionIP VARCHAR(45) = NULL,
    @MensajeError VARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Insertar en la bitácora
    INSERT INTO SesionesLog (UsuarioID, UsernameIngresado, Exitoso, OrigenAcceso, DireccionIP, MensajeError)
    VALUES (@UsuarioID, @UsernameIngresado, @Exitoso, @OrigenAcceso, @DireccionIP, @MensajeError);

    -- Si fue exitoso, resetear intentos fallidos y actualizar fecha
    IF @Exitoso = 1 AND @UsuarioID IS NOT NULL
    BEGIN
        UPDATE Usuarios 
        SET IntentosFallidos = 0,
            FechaUltimoAcceso = GETDATE()
        WHERE UsuarioID = @UsuarioID;
    END
    -- Si falló y el usuario existe, incrementar intentos y bloquear a los 5 intentos
    ELSE IF @Exitoso = 0 AND @UsuarioID IS NOT NULL
    BEGIN
        UPDATE Usuarios
        SET IntentosFallidos = IntentosFallidos + 1,
            Bloqueado = CASE WHEN IntentosFallidos + 1 >= 5 THEN 1 ELSE Bloqueado END
        WHERE UsuarioID = @UsuarioID;
    END
END;
GO

