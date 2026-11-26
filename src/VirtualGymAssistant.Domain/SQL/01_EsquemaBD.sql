/* =====================================================================
   VirtualGymAssistant 
   ---------------------------------------------------------------------

   ===================================================================== */

IF DB_ID('VirtualGymAssistant') IS NULL
    CREATE DATABASE [VirtualGymAssistant];
GO
USE [VirtualGymAssistant];
GO
SET NOCOUNT ON;
GO


SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Si corrés este script con sqlcmd (no SSMS), agregá "-f 65001": sin eso, la consola
-- puede mandar los literales N'...' con tildes en una codificación distinta a UTF-8,
-- y el MERGE de "15. CATÁLOGOS BASE" más abajo (que matchea por Nombre) no reconoce
-- la fila existente y la duplica con el texto corrompido. Ya pasó con Roles,
-- GruposMusculares y AlimentosCategorias en la base de desarrollo; se limpiaron a
-- mano, pero puede volver a pasar si se corre así de nuevo.

/* =====================================================================
   1. CATÁLOGOS GENERALES
   ===================================================================== */

IF OBJECT_ID('dbo.Zonas') IS NULL
CREATE TABLE dbo.Zonas (
    ID_Zona   INT IDENTITY(1,1) CONSTRAINT PK_Zonas PRIMARY KEY,
    Nombre    NVARCHAR(80)  NOT NULL CONSTRAINT UQ_Zonas_Nombre UNIQUE,
    Localidad NVARCHAR(80)  NULL,
    Provincia NVARCHAR(80)  NULL
);
GO

IF OBJECT_ID('dbo.Roles') IS NULL
CREATE TABLE dbo.Roles (
    ID_Rol      INT IDENTITY(1,1) CONSTRAINT PK_Roles PRIMARY KEY,
    Nombre      NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Roles_Nombre UNIQUE,
    Descripcion NVARCHAR(150) NULL
);
GO

IF OBJECT_ID('dbo.SocioEstados') IS NULL
CREATE TABLE dbo.SocioEstados (
    ID_Estado INT IDENTITY(1,1) CONSTRAINT PK_SocioEstados PRIMARY KEY,
    Nombre    NVARCHAR(30) NOT NULL CONSTRAINT UQ_SocioEstados_Nombre UNIQUE
);
GO

IF OBJECT_ID('dbo.PagoEstados') IS NULL
CREATE TABLE dbo.PagoEstados (
    ID_EstadoPago INT IDENTITY(1,1) CONSTRAINT PK_PagoEstados PRIMARY KEY,
    Nombre        NVARCHAR(30) NOT NULL CONSTRAINT UQ_PagoEstados_Nombre UNIQUE
);
GO

IF OBJECT_ID('dbo.Objetivos') IS NULL
CREATE TABLE dbo.Objetivos (
    ID_Objetivo INT IDENTITY(1,1) CONSTRAINT PK_Objetivos PRIMARY KEY,
    Nombre      NVARCHAR(50) NOT NULL CONSTRAINT UQ_Objetivos_Nombre UNIQUE
);
GO

IF OBJECT_ID('dbo.GruposMusculares') IS NULL
CREATE TABLE dbo.GruposMusculares (
    ID_Grupo INT IDENTITY(1,1) CONSTRAINT PK_GruposMusculares PRIMARY KEY,
    Nombre   NVARCHAR(50) NOT NULL CONSTRAINT UQ_GruposMusculares_Nombre UNIQUE
);
GO

IF OBJECT_ID('dbo.AlimentosCategorias') IS NULL
CREATE TABLE dbo.AlimentosCategorias (
    ID_Categoria INT IDENTITY(1,1) CONSTRAINT PK_AlimentosCategorias PRIMARY KEY,
    Nombre       NVARCHAR(50) NOT NULL CONSTRAINT UQ_AlimentosCategorias_Nombre UNIQUE
);
GO

IF OBJECT_ID('dbo.FormasPago') IS NULL
CREATE TABLE dbo.FormasPago (
    ID_FormaPago INT IDENTITY(1,1) CONSTRAINT PK_FormasPago PRIMARY KEY,
    Nombre       NVARCHAR(50) NOT NULL CONSTRAINT UQ_FormasPago_Nombre UNIQUE,
    Activo       BIT NOT NULL CONSTRAINT DF_FormasPago_Activo DEFAULT (1)
);
GO


/* =====================================================================
   2. USUARIOS  (login único: dueño, personal y socios)
   ===================================================================== */

IF OBJECT_ID('dbo.Usuarios') IS NULL
CREATE TABLE dbo.Usuarios (
    ID_Usuario INT IDENTITY(1,1) CONSTRAINT PK_Usuarios PRIMARY KEY,
    Username   NVARCHAR(50)  NOT NULL,
    Mail       NVARCHAR(254) NOT NULL,
    Nombre     NVARCHAR(60)  NULL,
    Apellido   NVARCHAR(60)  NULL,
    Telefono   NVARCHAR(30)  NULL,
    Clave      NVARCHAR(255) NOT NULL,
    Activo     BIT NOT NULL CONSTRAINT DF_Usuarios_Activo    DEFAULT (1),
    EmailVerificado BIT NOT NULL CONSTRAINT DF_Usuarios_EmailVerificado DEFAULT (0),
    FechaAlta  DATETIME2(0) NOT NULL CONSTRAINT DF_Usuarios_FechaAlta DEFAULT (SYSDATETIME()),
    CONSTRAINT UQ_Usuarios_Username UNIQUE (Username),
    CONSTRAINT UQ_Usuarios_Mail     UNIQUE (Mail)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_Usuarios_Telefono')
CREATE UNIQUE INDEX UX_Usuarios_Telefono
    ON dbo.Usuarios (Telefono) WHERE Telefono IS NOT NULL;
GO


IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Usuarios') AND name = 'EmailVerificado')
ALTER TABLE dbo.Usuarios ADD EmailVerificado BIT NOT NULL CONSTRAINT DF_Usuarios_EmailVerificado DEFAULT (0);
GO


/* =====================================================================
   3. ORGANIZACIÓN (el "dueño" / la cadena)  y  SUCURSALES
   ===================================================================== */

IF OBJECT_ID('dbo.Organizaciones') IS NULL
CREATE TABLE dbo.Organizaciones (
    ID_Organizacion INT IDENTITY(1,1) CONSTRAINT PK_Organizaciones PRIMARY KEY,
    Nombre          NVARCHAR(100) NOT NULL CONSTRAINT UQ_Organizaciones_Nombre UNIQUE,
    RazonSocial     NVARCHAR(150) NULL,
    CUIT            VARCHAR(13)   NULL,
    ID_Duenio       INT NOT NULL,
    Activo          BIT NOT NULL CONSTRAINT DF_Org_Activo    DEFAULT (1),
    FechaAlta       DATETIME2(0) NOT NULL CONSTRAINT DF_Org_FechaAlta DEFAULT (SYSDATETIME()),
    CONSTRAINT FK_Organizaciones_Duenio
        FOREIGN KEY (ID_Duenio) REFERENCES dbo.Usuarios (ID_Usuario)
);
GO

IF OBJECT_ID('dbo.Gimnasios') IS NULL
CREATE TABLE dbo.Gimnasios (
    ID_Gimnasio     INT IDENTITY(1,1) CONSTRAINT PK_Gimnasios PRIMARY KEY,
    ID_Organizacion INT NOT NULL,
    Nombre          NVARCHAR(80)  NOT NULL,
    Direccion       NVARCHAR(150) NOT NULL,
    Latitud         DECIMAL(9,6)  NULL,
    Longitud        DECIMAL(9,6)  NULL,
    ID_Zona         INT NULL,
    Telefono        NVARCHAR(30)  NULL,
    Email           NVARCHAR(254) NULL,
    EsCasaMatriz    BIT NOT NULL CONSTRAINT DF_Gimnasios_CasaMatriz DEFAULT (0),
    Activo          BIT NOT NULL CONSTRAINT DF_Gimnasios_Activo     DEFAULT (1),
    FechaAlta       DATETIME2(0) NOT NULL CONSTRAINT DF_Gimnasios_FechaAlta DEFAULT (SYSDATETIME()),
    CONSTRAINT FK_Gimnasios_Organizacion
        FOREIGN KEY (ID_Organizacion) REFERENCES dbo.Organizaciones (ID_Organizacion),
    CONSTRAINT FK_Gimnasios_Zona
        FOREIGN KEY (ID_Zona) REFERENCES dbo.Zonas (ID_Zona),
    CONSTRAINT UQ_Gimnasios_Org_Nombre UNIQUE (ID_Organizacion, Nombre)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Gimnasios_Organizacion')
CREATE INDEX IX_Gimnasios_Organizacion ON dbo.Gimnasios (ID_Organizacion);
GO

-- Ubicación en el mapa del sitio público. Son opcionales: una sucursal sin coordenadas se
-- muestra igual en las listas, sólo que sin pin. Para bases creadas antes de esta columna.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Gimnasios') AND name = 'Latitud')
ALTER TABLE dbo.Gimnasios ADD Latitud DECIMAL(9,6) NULL, Longitud DECIMAL(9,6) NULL;
GO

-- Van las dos o ninguna, y dentro del rango válido. Con "IS NOT NULL" explícito porque un
-- CHECK sólo falla cuando da FALSO: sin eso, una latitud sola daría "desconocido" y pasaría.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Gimnasios_Coordenadas')
ALTER TABLE dbo.Gimnasios ADD CONSTRAINT CK_Gimnasios_Coordenadas CHECK (
    (Latitud IS NULL AND Longitud IS NULL)
    OR (Latitud IS NOT NULL AND Longitud IS NOT NULL
        AND Latitud BETWEEN -90 AND 90 AND Longitud BETWEEN -180 AND 180)
);
GO


/* =====================================================================
   4. PERSONAL  -  rol de un usuario por sucursal
   ===================================================================== */

IF OBJECT_ID('dbo.UsuarioGimnasioRol') IS NULL
CREATE TABLE dbo.UsuarioGimnasioRol (
    ID_UGR      INT IDENTITY(1,1) CONSTRAINT PK_UsuarioGimnasioRol PRIMARY KEY,
    ID_Usuario  INT NOT NULL,
    ID_Gimnasio INT NOT NULL,
    ID_Rol      INT NOT NULL,
    FechaDesde  DATE NOT NULL CONSTRAINT DF_UGR_FechaDesde DEFAULT (CAST(SYSDATETIME() AS date)),
    FechaHasta  DATE NULL,
    Activo      BIT  NOT NULL CONSTRAINT DF_UGR_Activo DEFAULT (1),
    CONSTRAINT FK_UGR_Usuario  FOREIGN KEY (ID_Usuario)  REFERENCES dbo.Usuarios  (ID_Usuario),
    CONSTRAINT FK_UGR_Gimnasio FOREIGN KEY (ID_Gimnasio) REFERENCES dbo.Gimnasios (ID_Gimnasio),
    CONSTRAINT FK_UGR_Rol      FOREIGN KEY (ID_Rol)      REFERENCES dbo.Roles     (ID_Rol),
    CONSTRAINT UQ_UGR UNIQUE (ID_Usuario, ID_Gimnasio, ID_Rol)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_UGR_Gimnasio_Rol')
CREATE INDEX IX_UGR_Gimnasio_Rol ON dbo.UsuarioGimnasioRol (ID_Gimnasio, ID_Rol);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_UGR_Usuario')
CREATE INDEX IX_UGR_Usuario ON dbo.UsuarioGimnasioRol (ID_Usuario);
GO


/* =====================================================================
   5. EJERCICIOS Y MÁQUINAS
   ===================================================================== */

IF OBJECT_ID('dbo.Ejercicios') IS NULL
CREATE TABLE dbo.Ejercicios (
    ID_Ejercicio INT IDENTITY(1,1) CONSTRAINT PK_Ejercicios PRIMARY KEY,
    Nombre       NVARCHAR(100) NOT NULL CONSTRAINT UQ_Ejercicios_Nombre UNIQUE,
    Descripcion  NVARCHAR(500) NULL,
    ID_Grupo     INT NOT NULL,
    VideoUrl     NVARCHAR(300) NULL,
    CONSTRAINT FK_Ejercicios_Grupo
        FOREIGN KEY (ID_Grupo) REFERENCES dbo.GruposMusculares (ID_Grupo)
);
GO

IF OBJECT_ID('dbo.Maquinas') IS NULL
CREATE TABLE dbo.Maquinas (
    ID_Maquina  INT IDENTITY(1,1) CONSTRAINT PK_Maquinas PRIMARY KEY,
    Nombre      NVARCHAR(100) NOT NULL CONSTRAINT UQ_Maquinas_Nombre UNIQUE,
    Descripcion NVARCHAR(255) NULL,
    Foto        NVARCHAR(500) NULL
);
GO

IF OBJECT_ID('dbo.GimnasioMaquinas') IS NULL
CREATE TABLE dbo.GimnasioMaquinas (
    ID_GimnasioMaquina INT IDENTITY(1,1) CONSTRAINT PK_GimnasioMaquinas PRIMARY KEY,
    ID_Gimnasio INT NOT NULL,
    ID_Maquina  INT NOT NULL,
    Cantidad    INT NOT NULL CONSTRAINT DF_GimMaq_Cantidad DEFAULT (1),
    Estado      NVARCHAR(30) NOT NULL CONSTRAINT DF_GimMaq_Estado DEFAULT ('Operativa'),
    CONSTRAINT FK_GimMaq_Gimnasio FOREIGN KEY (ID_Gimnasio) REFERENCES dbo.Gimnasios (ID_Gimnasio) ON DELETE CASCADE,
    CONSTRAINT FK_GimMaq_Maquina  FOREIGN KEY (ID_Maquina)  REFERENCES dbo.Maquinas  (ID_Maquina),
    CONSTRAINT UQ_GimMaq UNIQUE (ID_Gimnasio, ID_Maquina),
    CONSTRAINT CK_GimMaq_Cantidad CHECK (Cantidad >= 0)
);
GO


/* =====================================================================
   6. ALIMENTOS
   ===================================================================== */

IF OBJECT_ID('dbo.Alimentos') IS NULL
CREATE TABLE dbo.Alimentos (
    ID_Alimento   INT IDENTITY(1,1) CONSTRAINT PK_Alimentos PRIMARY KEY,
    Nombre        NVARCHAR(100) NOT NULL CONSTRAINT UQ_Alimentos_Nombre UNIQUE,
    Calorias      DECIMAL(7,2)  NULL,
    Proteinas     DECIMAL(6,2)  NULL,
    Carbohidratos DECIMAL(6,2)  NULL,
    Grasas        DECIMAL(6,2)  NULL,
    Descripcion   NVARCHAR(255) NULL,
    ID_Categoria  INT NULL,
    ID_Objetivo   INT NULL,
    CONSTRAINT FK_Alimentos_Categoria FOREIGN KEY (ID_Categoria) REFERENCES dbo.AlimentosCategorias (ID_Categoria),
    CONSTRAINT FK_Alimentos_Objetivo  FOREIGN KEY (ID_Objetivo)  REFERENCES dbo.Objetivos          (ID_Objetivo),
    CONSTRAINT CK_Alimentos_NoNegativos
        CHECK (Calorias >= 0 AND Proteinas >= 0 AND Carbohidratos >= 0 AND Grasas >= 0)
);
GO


/* =====================================================================
   7. SOCIOS  (miembros)  +  acceso multi-sucursal
   ===================================================================== */

IF OBJECT_ID('dbo.Socios') IS NULL
CREATE TABLE dbo.Socios (
    ID_Socio        INT IDENTITY(1,1) CONSTRAINT PK_Socios PRIMARY KEY,
    ID_Usuario      INT NOT NULL,
    ID_Gimnasio     INT NOT NULL,                
    NumeroSocio     INT NULL,
    ID_Estado       INT NOT NULL,
    ID_Objetivo     INT NULL,
    FechaNacimiento DATE NULL,
    FechaAlta       DATE NOT NULL CONSTRAINT DF_Socios_FechaAlta DEFAULT (CAST(SYSDATETIME() AS date)),
    FechaBaja       DATE NULL,
    Observaciones   NVARCHAR(500) NULL,
    CONSTRAINT FK_Socios_Usuario  FOREIGN KEY (ID_Usuario)  REFERENCES dbo.Usuarios     (ID_Usuario),
    CONSTRAINT FK_Socios_Gimnasio FOREIGN KEY (ID_Gimnasio) REFERENCES dbo.Gimnasios    (ID_Gimnasio),
    CONSTRAINT FK_Socios_Estado   FOREIGN KEY (ID_Estado)   REFERENCES dbo.SocioEstados (ID_Estado),
    CONSTRAINT FK_Socios_Objetivo FOREIGN KEY (ID_Objetivo) REFERENCES dbo.Objetivos    (ID_Objetivo),
    CONSTRAINT UQ_Socios_Usuario_Gimnasio UNIQUE (ID_Usuario, ID_Gimnasio)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_Socios_Numero')
CREATE UNIQUE INDEX UX_Socios_Numero
    ON dbo.Socios (ID_Gimnasio, NumeroSocio) WHERE NumeroSocio IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Socios_Gimnasio_Estado')
CREATE INDEX IX_Socios_Gimnasio_Estado ON dbo.Socios (ID_Gimnasio, ID_Estado);
GO

IF OBJECT_ID('dbo.SocioAccesoGimnasio') IS NULL
CREATE TABLE dbo.SocioAccesoGimnasio (
    ID_Socio    INT NOT NULL,
    ID_Gimnasio INT NOT NULL,
    FechaDesde  DATE NOT NULL CONSTRAINT DF_SAG_FechaDesde DEFAULT (CAST(SYSDATETIME() AS date)),
    CONSTRAINT PK_SocioAccesoGimnasio PRIMARY KEY (ID_Socio, ID_Gimnasio),
    CONSTRAINT FK_SAG_Socio    FOREIGN KEY (ID_Socio)    REFERENCES dbo.Socios    (ID_Socio) ON DELETE CASCADE,
    CONSTRAINT FK_SAG_Gimnasio FOREIGN KEY (ID_Gimnasio) REFERENCES dbo.Gimnasios (ID_Gimnasio)
);
GO


/* =====================================================================
   8. PLANES, MEMBRESÍAS Y PAGOS
   ===================================================================== */

IF OBJECT_ID('dbo.Planes') IS NULL
CREATE TABLE dbo.Planes (
    ID_Plan         INT IDENTITY(1,1) CONSTRAINT PK_Planes PRIMARY KEY,
    ID_Organizacion INT NOT NULL,
    Nombre          NVARCHAR(100) NOT NULL,
    Descripcion     NVARCHAR(255) NULL,
    Precio          DECIMAL(12,2) NOT NULL,
    DuracionDias    INT NOT NULL CONSTRAINT DF_Planes_Duracion DEFAULT (30),
    Nivel           NVARCHAR(50)  NULL,
    AccesoMultiSede BIT NOT NULL CONSTRAINT DF_Planes_MultiSede DEFAULT (0),
    Activo          BIT NOT NULL CONSTRAINT DF_Planes_Activo DEFAULT (1),
    CONSTRAINT FK_Planes_Organizacion FOREIGN KEY (ID_Organizacion) REFERENCES dbo.Organizaciones (ID_Organizacion),
    CONSTRAINT UQ_Planes_Org_Nombre UNIQUE (ID_Organizacion, Nombre),
    CONSTRAINT CK_Planes_Precio CHECK (Precio >= 0)
);
GO

IF OBJECT_ID('dbo.Membresias') IS NULL
CREATE TABLE dbo.Membresias (
    ID_Membresia   INT IDENTITY(1,1) CONSTRAINT PK_Membresias PRIMARY KEY,
    ID_Socio       INT NOT NULL,
    ID_Plan        INT NOT NULL,
    ID_Gimnasio    INT NOT NULL,
    FechaInicio    DATE NOT NULL,
    FechaFin       DATE NULL,
    PrecioAcordado DECIMAL(12,2) NOT NULL,
    Activa         BIT NOT NULL CONSTRAINT DF_Membresias_Activa DEFAULT (1),
    CONSTRAINT FK_Membresias_Socio    FOREIGN KEY (ID_Socio)    REFERENCES dbo.Socios    (ID_Socio),
    CONSTRAINT FK_Membresias_Plan     FOREIGN KEY (ID_Plan)     REFERENCES dbo.Planes    (ID_Plan),
    CONSTRAINT FK_Membresias_Gimnasio FOREIGN KEY (ID_Gimnasio) REFERENCES dbo.Gimnasios (ID_Gimnasio),
    CONSTRAINT CK_Membresias_Fechas CHECK (FechaFin IS NULL OR FechaFin >= FechaInicio)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_Membresias_SocioActiva')
CREATE UNIQUE INDEX UX_Membresias_SocioActiva ON dbo.Membresias (ID_Socio) WHERE Activa = 1;
GO

IF OBJECT_ID('dbo.Pagos') IS NULL
CREATE TABLE dbo.Pagos (
    ID_Pago          INT IDENTITY(1,1) CONSTRAINT PK_Pagos PRIMARY KEY,
    ID_Socio         INT NOT NULL,
    ID_Gimnasio      INT NOT NULL,
    ID_Membresia     INT NULL,
    ID_Plan          INT NULL,
    ID_FormaPago     INT NOT NULL,
    PeriodoAnio      SMALLINT NOT NULL,
    PeriodoMes       TINYINT  NOT NULL,
    Monto            DECIMAL(12,2) NOT NULL,
    ID_EstadoPago    INT NOT NULL,
    FechaVencimiento DATE NULL,
    FechaPago        DATETIME2(0) NULL,
    FechaRegistro    DATETIME2(0) NOT NULL CONSTRAINT DF_Pagos_FechaRegistro DEFAULT (SYSDATETIME()),
    Comprobante      NVARCHAR(50) NULL,
    Observaciones    NVARCHAR(255) NULL,
    CONSTRAINT FK_Pagos_Socio      FOREIGN KEY (ID_Socio)      REFERENCES dbo.Socios      (ID_Socio),
    CONSTRAINT FK_Pagos_Gimnasio   FOREIGN KEY (ID_Gimnasio)   REFERENCES dbo.Gimnasios   (ID_Gimnasio),
    CONSTRAINT FK_Pagos_Membresia  FOREIGN KEY (ID_Membresia)  REFERENCES dbo.Membresias  (ID_Membresia),
    CONSTRAINT FK_Pagos_Plan       FOREIGN KEY (ID_Plan)       REFERENCES dbo.Planes      (ID_Plan),
    CONSTRAINT FK_Pagos_FormaPago  FOREIGN KEY (ID_FormaPago)  REFERENCES dbo.FormasPago  (ID_FormaPago),
    CONSTRAINT FK_Pagos_Estado     FOREIGN KEY (ID_EstadoPago) REFERENCES dbo.PagoEstados (ID_EstadoPago),
    CONSTRAINT CK_Pagos_Monto CHECK (Monto >= 0),
    CONSTRAINT CK_Pagos_Mes   CHECK (PeriodoMes BETWEEN 1 AND 12)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_Pagos_Socio_Periodo')
CREATE UNIQUE INDEX UX_Pagos_Socio_Periodo
    ON dbo.Pagos (ID_Socio, PeriodoAnio, PeriodoMes);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Pagos_Gimnasio_Estado')
CREATE INDEX IX_Pagos_Gimnasio_Estado ON dbo.Pagos (ID_Gimnasio, ID_EstadoPago);
GO


/* =====================================================================
   9. ASISTENCIAS  (check-in por sucursal)
   ===================================================================== */

IF OBJECT_ID('dbo.Asistencias') IS NULL
CREATE TABLE dbo.Asistencias (
    ID_Asistencia    INT IDENTITY(1,1) CONSTRAINT PK_Asistencias PRIMARY KEY,
    ID_Socio         INT NOT NULL,
    ID_Gimnasio      INT NOT NULL,
    FechaHoraEntrada DATETIME2(0) NOT NULL CONSTRAINT DF_Asistencias_Entrada DEFAULT (SYSDATETIME()),
    FechaHoraSalida  DATETIME2(0) NULL,
    Fecha AS (CAST(FechaHoraEntrada AS date)) PERSISTED,
    CONSTRAINT FK_Asistencias_Socio    FOREIGN KEY (ID_Socio)    REFERENCES dbo.Socios    (ID_Socio),
    CONSTRAINT FK_Asistencias_Gimnasio FOREIGN KEY (ID_Gimnasio) REFERENCES dbo.Gimnasios (ID_Gimnasio),
    CONSTRAINT CK_Asistencias_Salida CHECK (FechaHoraSalida IS NULL OR FechaHoraSalida >= FechaHoraEntrada)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Asistencias_Gimnasio_Fecha')
CREATE INDEX IX_Asistencias_Gimnasio_Fecha ON dbo.Asistencias (ID_Gimnasio, Fecha);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Asistencias_Socio_Fecha')
CREATE INDEX IX_Asistencias_Socio_Fecha ON dbo.Asistencias (ID_Socio, Fecha);
GO


/* =====================================================================
   10. RUTINAS  (plantillas)  +  rutina asignada al socio
   ===================================================================== */

IF OBJECT_ID('dbo.Rutinas') IS NULL
CREATE TABLE dbo.Rutinas (
    ID_Rutina        INT IDENTITY(1,1) CONSTRAINT PK_Rutinas PRIMARY KEY,
    ID_Organizacion  INT NULL,                    
    Nombre           NVARCHAR(100) NOT NULL,
    Descripcion      NVARCHAR(255) NULL,
    Intensidad       NVARCHAR(50)  NULL,
    ID_Objetivo      INT NULL,
    DiasPorSemana    INT NULL,
    EjerciciosPorDia INT NULL,
    Activa           BIT NOT NULL CONSTRAINT DF_Rutinas_Activa DEFAULT (1),
    CONSTRAINT FK_Rutinas_Organizacion FOREIGN KEY (ID_Organizacion) REFERENCES dbo.Organizaciones (ID_Organizacion),
    CONSTRAINT FK_Rutinas_Objetivo     FOREIGN KEY (ID_Objetivo)     REFERENCES dbo.Objetivos      (ID_Objetivo)
);
GO

IF OBJECT_ID('dbo.RutinaDetalle') IS NULL
CREATE TABLE dbo.RutinaDetalle (
    ID_RutinaDetalle INT IDENTITY(1,1) CONSTRAINT PK_RutinaDetalle PRIMARY KEY,
    ID_Rutina    INT NOT NULL,
    Dia          TINYINT NOT NULL,
    Orden        INT NOT NULL CONSTRAINT DF_RutDet_Orden DEFAULT (1),
    ID_Ejercicio INT NOT NULL,
    Series       INT NULL,
    Repeticiones NVARCHAR(20) NULL,
    DescansoSeg  INT NULL,
    CONSTRAINT FK_RutDet_Rutina    FOREIGN KEY (ID_Rutina)    REFERENCES dbo.Rutinas    (ID_Rutina) ON DELETE CASCADE,
    CONSTRAINT FK_RutDet_Ejercicio FOREIGN KEY (ID_Ejercicio) REFERENCES dbo.Ejercicios (ID_Ejercicio)
);
GO

IF OBJECT_ID('dbo.SocioRutinas') IS NULL
CREATE TABLE dbo.SocioRutinas (
    ID_SocioRutina INT IDENTITY(1,1) CONSTRAINT PK_SocioRutinas PRIMARY KEY,
    ID_Socio       INT NOT NULL,
    ID_Rutina      INT NULL,
    ID_Entrenador  INT NULL,
    Nombre         NVARCHAR(100) NOT NULL,
    FechaInicio    DATE NOT NULL CONSTRAINT DF_SocioRutinas_Inicio DEFAULT (CAST(SYSDATETIME() AS date)),
    FechaFin       DATE NULL,
    Activa         BIT NOT NULL CONSTRAINT DF_SocioRutinas_Activa DEFAULT (1),
    CONSTRAINT FK_SocioRutinas_Socio      FOREIGN KEY (ID_Socio)      REFERENCES dbo.Socios   (ID_Socio),
    CONSTRAINT FK_SocioRutinas_Rutina     FOREIGN KEY (ID_Rutina)     REFERENCES dbo.Rutinas  (ID_Rutina),
    CONSTRAINT FK_SocioRutinas_Entrenador FOREIGN KEY (ID_Entrenador) REFERENCES dbo.Usuarios (ID_Usuario)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_SocioRutinas_Socio')
CREATE INDEX IX_SocioRutinas_Socio ON dbo.SocioRutinas (ID_Socio);
GO

IF OBJECT_ID('dbo.SocioRutinaDetalle') IS NULL
CREATE TABLE dbo.SocioRutinaDetalle (
    ID_SocioRutinaDetalle INT IDENTITY(1,1) CONSTRAINT PK_SocioRutinaDetalle PRIMARY KEY,
    ID_SocioRutina INT NOT NULL,
    Dia          INT NOT NULL,
    Orden        INT NOT NULL CONSTRAINT DF_SRD_Orden DEFAULT (1),
    ID_Ejercicio INT NOT NULL,
    Series       INT NULL,
    Repeticiones NVARCHAR(20) NULL,
    DescansoSeg  INT NULL,
    Notas        NVARCHAR(255) NULL,
    CONSTRAINT FK_SRD_SocioRutina FOREIGN KEY (ID_SocioRutina) REFERENCES dbo.SocioRutinas (ID_SocioRutina) ON DELETE CASCADE,
    CONSTRAINT FK_SRD_Ejercicio   FOREIGN KEY (ID_Ejercicio)   REFERENCES dbo.Ejercicios   (ID_Ejercicio)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_SocioRutinaDetalle_Rutina')
CREATE INDEX IX_SocioRutinaDetalle_Rutina ON dbo.SocioRutinaDetalle (ID_SocioRutina, Dia, Orden);
GO


/* =====================================================================
   11. DIETAS asignadas al socio
   ===================================================================== */

IF OBJECT_ID('dbo.SocioDietas') IS NULL
CREATE TABLE dbo.SocioDietas (
    ID_SocioDieta    INT IDENTITY(1,1) CONSTRAINT PK_SocioDietas PRIMARY KEY,
    ID_Socio         INT NOT NULL,
    ID_Nutricionista INT NULL,
    Nombre           NVARCHAR(100) NOT NULL CONSTRAINT DF_SocioDietas_Nombre DEFAULT ('Dieta personalizada'),
    ID_Objetivo      INT NULL,
    FechaInicio      DATE NOT NULL CONSTRAINT DF_SocioDietas_Inicio DEFAULT (CAST(SYSDATETIME() AS date)),
    FechaFin         DATE NULL,
    Activa           BIT NOT NULL CONSTRAINT DF_SocioDietas_Activa DEFAULT (1),
    CONSTRAINT FK_SocioDietas_Socio         FOREIGN KEY (ID_Socio)         REFERENCES dbo.Socios    (ID_Socio),
    CONSTRAINT FK_SocioDietas_Nutricionista FOREIGN KEY (ID_Nutricionista) REFERENCES dbo.Usuarios  (ID_Usuario),
    CONSTRAINT FK_SocioDietas_Objetivo      FOREIGN KEY (ID_Objetivo)      REFERENCES dbo.Objetivos (ID_Objetivo)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_SocioDietas_Socio')
CREATE INDEX IX_SocioDietas_Socio ON dbo.SocioDietas (ID_Socio);
GO

IF OBJECT_ID('dbo.SocioDietaDetalle') IS NULL
CREATE TABLE dbo.SocioDietaDetalle (
    ID_SocioDietaDetalle INT IDENTITY(1,1) CONSTRAINT PK_SocioDietaDetalle PRIMARY KEY,
    ID_SocioDieta INT NOT NULL,
    Dia           NVARCHAR(20)  NOT NULL,          
    Categoria     NVARCHAR(50)  NOT NULL,           
    Alimento      NVARCHAR(150) NOT NULL,
    ID_Alimento   INT NULL,
    CONSTRAINT FK_SDD_SocioDieta FOREIGN KEY (ID_SocioDieta) REFERENCES dbo.SocioDietas (ID_SocioDieta) ON DELETE CASCADE,
    CONSTRAINT FK_SDD_Alimento   FOREIGN KEY (ID_Alimento)   REFERENCES dbo.Alimentos   (ID_Alimento)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_SocioDietaDetalle')
CREATE INDEX IX_SocioDietaDetalle ON dbo.SocioDietaDetalle (ID_SocioDieta, Dia);
GO


/* =====================================================================
   12. MEDICIONES / PROGRESO
   ===================================================================== */

IF OBJECT_ID('dbo.Mediciones') IS NULL
CREATE TABLE dbo.Mediciones (
    ID_Medicion      INT IDENTITY(1,1) CONSTRAINT PK_Mediciones PRIMARY KEY,
    ID_Socio         INT NOT NULL,
    Fecha            DATE NOT NULL CONSTRAINT DF_Mediciones_Fecha DEFAULT (CAST(SYSDATETIME() AS date)),
    Peso             DECIMAL(5,2) NOT NULL,
    GrasaPct         DECIMAL(5,2) NULL,
    MusculoPct       DECIMAL(5,2) NULL,
    Cintura          DECIMAL(5,2) NULL,
    Cadera           DECIMAL(5,2) NULL,
    Pecho            DECIMAL(5,2) NULL,
    Brazo            DECIMAL(5,2) NULL,
    Pierna           DECIMAL(5,2) NULL,
    ID_RegistradoPor INT NULL,
    Observaciones    NVARCHAR(255) NULL,
    CONSTRAINT FK_Mediciones_Socio         FOREIGN KEY (ID_Socio)         REFERENCES dbo.Socios   (ID_Socio),
    CONSTRAINT FK_Mediciones_RegistradoPor FOREIGN KEY (ID_RegistradoPor) REFERENCES dbo.Usuarios (ID_Usuario),
    CONSTRAINT CK_Mediciones_Peso CHECK (Peso > 0)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Mediciones_Socio_Fecha')
CREATE INDEX IX_Mediciones_Socio_Fecha ON dbo.Mediciones (ID_Socio, Fecha DESC);
GO


/* =====================================================================
   13. HORARIOS Y CONTENIDO WEB  (por sucursal / por organización)
   ===================================================================== */

IF OBJECT_ID('dbo.Horarios') IS NULL
CREATE TABLE dbo.Horarios (
    ID_Horario   INT IDENTITY(1,1) CONSTRAINT PK_Horarios PRIMARY KEY,
    ID_Gimnasio  INT NOT NULL,
    DiaSemana    TINYINT NOT NULL,                
    HoraApertura TIME(0) NOT NULL,
    HoraCierre   TIME(0) NOT NULL,
    Orden        INT NULL,
    CONSTRAINT FK_Horarios_Gimnasio FOREIGN KEY (ID_Gimnasio) REFERENCES dbo.Gimnasios (ID_Gimnasio) ON DELETE CASCADE,
    CONSTRAINT CK_Horarios_Dia CHECK (DiaSemana BETWEEN 1 AND 7)
);
GO

IF OBJECT_ID('dbo.CarouselItems') IS NULL
CREATE TABLE dbo.CarouselItems (
    ID_Carousel     INT IDENTITY(1,1) CONSTRAINT PK_CarouselItems PRIMARY KEY,
    ID_Organizacion INT NOT NULL,
    ID_Gimnasio     INT NULL,
    Tipo            NVARCHAR(50)  NULL,
    Titulo          NVARCHAR(100) NULL,
    Descripcion     NVARCHAR(255) NULL,
    Imagen          NVARCHAR(300) NULL,
    Orden           INT NULL,
    Activo          BIT NOT NULL CONSTRAINT DF_Carousel_Activo DEFAULT (1),
    CONSTRAINT FK_Carousel_Organizacion FOREIGN KEY (ID_Organizacion) REFERENCES dbo.Organizaciones (ID_Organizacion) ON DELETE CASCADE,
    CONSTRAINT FK_Carousel_Gimnasio     FOREIGN KEY (ID_Gimnasio)     REFERENCES dbo.Gimnasios     (ID_Gimnasio)
);
GO

IF OBJECT_ID('dbo.FooterConfig') IS NULL
CREATE TABLE dbo.FooterConfig (
    ID_Footer       INT IDENTITY(1,1) CONSTRAINT PK_FooterConfig PRIMARY KEY,
    ID_Organizacion INT NOT NULL,
    Seccion         NVARCHAR(50)  NOT NULL,
    Clave           NVARCHAR(50)  NOT NULL,
    Valor           NVARCHAR(255) NULL,
    Orden           INT NULL,
    CONSTRAINT FK_Footer_Organizacion FOREIGN KEY (ID_Organizacion) REFERENCES dbo.Organizaciones (ID_Organizacion) ON DELETE CASCADE
);
GO

IF OBJECT_ID('dbo.ForoPosts') IS NULL
CREATE TABLE dbo.ForoPosts (
    ID_Post         INT IDENTITY(1,1) CONSTRAINT PK_ForoPosts PRIMARY KEY,
    ID_Organizacion INT NOT NULL,
    ID_Gimnasio     INT NULL,
    ID_Usuario      INT NOT NULL,
    ID_PostPadre    INT NULL,
    Contenido       NVARCHAR(MAX) NULL,
    Archivo         NVARCHAR(300) NULL,
    Fecha           DATETIME2(0) NOT NULL CONSTRAINT DF_ForoPosts_Fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT FK_ForoPosts_Organizacion FOREIGN KEY (ID_Organizacion) REFERENCES dbo.Organizaciones (ID_Organizacion),
    CONSTRAINT FK_ForoPosts_Gimnasio     FOREIGN KEY (ID_Gimnasio)     REFERENCES dbo.Gimnasios     (ID_Gimnasio),
    CONSTRAINT FK_ForoPosts_Usuario      FOREIGN KEY (ID_Usuario)      REFERENCES dbo.Usuarios      (ID_Usuario),
    CONSTRAINT FK_ForoPosts_Padre        FOREIGN KEY (ID_PostPadre)    REFERENCES dbo.ForoPosts     (ID_Post)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_ForoPosts_Gimnasio_Fecha')
CREATE INDEX IX_ForoPosts_Gimnasio_Fecha ON dbo.ForoPosts (ID_Gimnasio, Fecha DESC);
GO


/* =====================================================================
   14. SEGURIDAD  -  recuperación de contraseña y verificación de email
   ===================================================================== */

IF OBJECT_ID('dbo.TokensRecuperacion') IS NULL
CREATE TABLE dbo.TokensRecuperacion (
    ID_Token      INT IDENTITY(1,1) CONSTRAINT PK_TokensRecuperacion PRIMARY KEY,
    ID_Usuario    INT NOT NULL,
    Token         NVARCHAR(255) NULL,
    Codigo        VARCHAR(10)   NULL,
    Canal         NVARCHAR(10)  NOT NULL CONSTRAINT DF_Tokens_Canal DEFAULT ('Mail'),
    Expira        DATETIME2(0)  NOT NULL,
    Usado         BIT NOT NULL CONSTRAINT DF_Tokens_Usado DEFAULT (0),
    FechaCreacion DATETIME2(0) NOT NULL CONSTRAINT DF_Tokens_FechaCreacion DEFAULT (SYSDATETIME()),
    CONSTRAINT FK_Tokens_Usuario FOREIGN KEY (ID_Usuario) REFERENCES dbo.Usuarios (ID_Usuario) ON DELETE CASCADE,
    CONSTRAINT CK_Tokens_Canal CHECK (Canal IN ('Mail','SMS'))
);
GO

-- Código de un solo uso para confirmar la casilla de correo del usuario. Es una
-- tabla aparte y no un "Proposito" más en TokensRecuperacion porque esa tabla ya
-- está acoplada a la recuperación de clave (nombre, comentarios, Canal Mail/SMS):
-- separarla mantiene un concepto por tabla, igual que el resto del esquema.
IF OBJECT_ID('dbo.TokensVerificacionEmail') IS NULL
CREATE TABLE dbo.TokensVerificacionEmail (
    ID_Token      INT IDENTITY(1,1) CONSTRAINT PK_TokensVerificacionEmail PRIMARY KEY,
    ID_Usuario    INT NOT NULL,
    Codigo        VARCHAR(10)  NOT NULL,
    Expira        DATETIME2(0) NOT NULL,
    Usado         BIT NOT NULL CONSTRAINT DF_TokensVerifEmail_Usado DEFAULT (0),
    FechaCreacion DATETIME2(0) NOT NULL CONSTRAINT DF_TokensVerifEmail_FechaCreacion DEFAULT (SYSDATETIME()),
    CONSTRAINT FK_TokensVerifEmail_Usuario FOREIGN KEY (ID_Usuario) REFERENCES dbo.Usuarios (ID_Usuario) ON DELETE CASCADE
);
GO


/* =====================================================================
   15. CATÁLOGOS BASE  (idempotente)
   ===================================================================== */

MERGE dbo.Roles AS t
USING (VALUES
    (N'Dueño',N'Titular de la organización, ve todas las sucursales'),
    (N'Administrador',N'Gestión completa de una o varias sucursales'),
    (N'Recepcionista',N'Alta de socios, cobros y control de acceso'),
    (N'Entrenador',N'Arma y asigna rutinas'),
    (N'Nutricionista',N'Arma y asigna dietas'),
    (N'Socio',N'Miembro del gimnasio')
) AS s (Nombre, Descripcion)
ON t.Nombre = s.Nombre
WHEN NOT MATCHED THEN INSERT (Nombre, Descripcion) VALUES (s.Nombre, s.Descripcion);

MERGE dbo.SocioEstados AS t
USING (VALUES (N'Activo'),(N'Suspendido'),(N'Moroso'),(N'Baja')) AS s (Nombre)
ON t.Nombre = s.Nombre
WHEN NOT MATCHED THEN INSERT (Nombre) VALUES (s.Nombre);

MERGE dbo.PagoEstados AS t
USING (VALUES (N'Pendiente'),(N'Pagado'),(N'Rechazado'),(N'Vencido'),(N'Anulado')) AS s (Nombre)
ON t.Nombre = s.Nombre
WHEN NOT MATCHED THEN INSERT (Nombre) VALUES (s.Nombre);

MERGE dbo.Objetivos AS t
USING (VALUES (N'Ganar masa muscular'),(N'Perder grasa'),(N'Mantenimiento'),(N'Resistencia'),(N'Fuerza')) AS s (Nombre)
ON t.Nombre = s.Nombre
WHEN NOT MATCHED THEN INSERT (Nombre) VALUES (s.Nombre);

MERGE dbo.GruposMusculares AS t
USING (VALUES (N'Pecho'),(N'Espalda'),(N'Piernas'),(N'Glúteos'),(N'Hombros'),(N'Bíceps'),(N'Tríceps'),(N'Core')) AS s (Nombre)
ON t.Nombre = s.Nombre
WHEN NOT MATCHED THEN INSERT (Nombre) VALUES (s.Nombre);

MERGE dbo.AlimentosCategorias AS t
USING (VALUES (N'Proteínas'),(N'Carbohidratos'),(N'Grasas'),(N'Verduras'),(N'Frutas'),(N'Lácteos'),(N'Legumbres'),(N'Bebidas')) AS s (Nombre)
ON t.Nombre = s.Nombre
WHEN NOT MATCHED THEN INSERT (Nombre) VALUES (s.Nombre);

MERGE dbo.FormasPago AS t
USING (VALUES (N'Efectivo'),(N'Débito'),(N'Crédito'),(N'Transferencia'),(N'Mercado Pago')) AS s (Nombre)
ON t.Nombre = s.Nombre
WHEN NOT MATCHED THEN INSERT (Nombre) VALUES (s.Nombre);
GO


/* =====================================================================
   16. VISTAS DE APOYO
   ===================================================================== */

CREATE OR ALTER VIEW dbo.vw_PadronSocios AS
SELECT  s.ID_Socio, o.ID_Organizacion, o.Nombre AS Organizacion,
        g.ID_Gimnasio, g.Nombre AS Sucursal, s.NumeroSocio,
        u.ID_Usuario, u.Username, u.Nombre + ' ' + u.Apellido AS NombreCompleto,
        u.Mail, u.Telefono, se.Nombre AS Estado,
        p.Nombre AS PlanActivo, m.FechaInicio AS PlanDesde, m.FechaFin AS PlanHasta
FROM        dbo.Socios        s
JOIN        dbo.Usuarios      u  ON u.ID_Usuario     = s.ID_Usuario
JOIN        dbo.Gimnasios     g  ON g.ID_Gimnasio    = s.ID_Gimnasio
JOIN        dbo.Organizaciones o ON o.ID_Organizacion = g.ID_Organizacion
JOIN        dbo.SocioEstados  se ON se.ID_Estado     = s.ID_Estado
LEFT JOIN   dbo.Membresias    m  ON m.ID_Socio       = s.ID_Socio AND m.Activa = 1
LEFT JOIN   dbo.Planes        p  ON p.ID_Plan        = m.ID_Plan;
GO

CREATE OR ALTER VIEW dbo.vw_Sucursales AS
SELECT  o.ID_Organizacion, o.Nombre AS Organizacion,
        du.ID_Usuario AS ID_Duenio, du.Nombre + ' ' + du.Apellido AS Duenio,
        g.ID_Gimnasio, g.Nombre AS Sucursal, g.Direccion, z.Nombre AS Zona,
        g.EsCasaMatriz, g.Activo
FROM        dbo.Organizaciones o
JOIN        dbo.Usuarios       du ON du.ID_Usuario = o.ID_Duenio
JOIN        dbo.Gimnasios      g  ON g.ID_Organizacion = o.ID_Organizacion
LEFT JOIN   dbo.Zonas          z  ON z.ID_Zona = g.ID_Zona;
GO

PRINT 'VirtualGymAssistant v2 - esquema creado / actualizado.';
GO
