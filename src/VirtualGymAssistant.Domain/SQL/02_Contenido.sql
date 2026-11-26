/* =====================================================================
   VirtualGymAssistant v2  
   --------------------------------------------------------------------
   Contraseña de todos los usuarios:  123456
   =====================================================================
   Usuarios:
     duenio@vga.com         Dueño (ve las 3 sucursales)
     admin@vga.com          Administrador (las 3 sucursales)
     recepcion@vga.com      Recepcionista (Casa Central)
     entrenador@vga.com     Entrenador (Casa Central + Norte)
     nutricionista@vga.com  Nutricionista (Casa Central)
     socio@vga.com  +  lucia / martin / julieta / nicolas / tomas / valentina / camila
   ===================================================================== */

USE [VirtualGymAssistant];
GO

-- Hace falta explícitamente porque la tabla Usuarios tiene un índice único
-- filtrado (UX_Usuarios_Telefono): sin esto, insertar o actualizar filas ahí
-- falla con el error 1934. SSMS lo activa solo en cada ventana nueva; sqlcmd no.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- Corre siempre, haya o no que insertar el resto: así una base que ya tenía
-- estas cuentas demo (de antes de agregar la verificación de email) también
-- las deja marcadas como verificadas, y la demo no queda tapada por el aviso.
UPDATE dbo.Usuarios SET EmailVerificado = 1
WHERE Mail IN (
    N'duenio@vga.com', N'admin@vga.com', N'recepcion@vga.com', N'entrenador@vga.com',
    N'nutricionista@vga.com', N'socio@vga.com', N'lucia@vga.com', N'martin@vga.com',
    N'julieta@vga.com', N'nicolas@vga.com', N'tomas@vga.com', N'valentina@vga.com', N'camila@vga.com'
);
GO

-- Corre siempre, por la misma razón: una base que ya tenía las sedes demo (de antes de
-- agregar las coordenadas) las deja ubicadas en el mapa. Sólo completa las que siguen sin
-- coordenadas, así nunca pisa una ubicación que alguien ya corrigió a mano. La de Sucursal
-- Sur es aproximada: OpenStreetMap no tiene el número 900 de esa avenida, así que el pin
-- cae sobre la avenida, en Nueva Córdoba.
UPDATE dbo.Gimnasios SET Latitud = -31.408571, Longitud = -64.199845
WHERE Direccion = N'Av. Colón 1234, Córdoba' AND Latitud IS NULL;
UPDATE dbo.Gimnasios SET Latitud = -31.364694, Longitud = -64.235999
WHERE Direccion = N'Av. Rafael Núñez 4567, Córdoba' AND Latitud IS NULL;
UPDATE dbo.Gimnasios SET Latitud = -31.429559, Longitud = -64.192753
WHERE Direccion = N'Av. Vélez Sarsfield 900, Córdoba' AND Latitud IS NULL;
GO

IF EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Mail = N'duenio@vga.com')
BEGIN
    PRINT '>> La base ya tiene datos demo. No se insertó nada.';
    RETURN;
END;

BEGIN TRANSACTION;

DECLARE @hash NVARCHAR(255) = '$2b$10$qFAYTDR36kNv7lbYTLC5Qu6MQBK7/Q2YNkz.hXC29VdkhavfuqKDO'; -- 123456
DECLARE @hoy DATE = CAST(SYSDATETIME() AS date);
DECLARE @anio INT = YEAR(@hoy);
DECLARE @mes  INT = MONTH(@hoy);

/* ============================ USUARIOS ============================ */
INSERT dbo.Usuarios (Nombre, Apellido, Mail, Telefono, Clave, Username, EmailVerificado) VALUES
 (N'Ignacio',  N'Blanco',    N'duenio@vga.com',        N'3510000001', @hash, N'duenio', 1),
 (N'Ana',      N'Gómez',     N'admin@vga.com',         N'3510000002', @hash, N'admin', 1),
 (N'Sofía',    N'López',     N'recepcion@vga.com',     N'3510000003', @hash, N'recepcion', 1),
 (N'Bruno',    N'Díaz',      N'entrenador@vga.com',    N'3510000004', @hash, N'entrenador', 1),
 (N'Carla',    N'Ruiz',      N'nutricionista@vga.com', N'3510000005', @hash, N'nutricionista', 1),
 (N'Diego',    N'Pérez',     N'socio@vga.com',         N'3510000010', @hash, N'socio', 1),
 (N'Lucía',    N'Fernández', N'lucia@vga.com',         N'3510000011', @hash, N'lucia', 1),
 (N'Martín',   N'Sosa',      N'martin@vga.com',        N'3510000012', @hash, N'martin', 1),
 (N'Julieta',  N'Romero',    N'julieta@vga.com',       N'3510000013', @hash, N'julieta', 1),
 (N'Nicolás',  N'Torres',    N'nicolas@vga.com',       N'3510000014', @hash, N'nicolas', 1),
 (N'Tomás',    N'Herrera',   N'tomas@vga.com',         N'3510000015', @hash, N'tomas', 1),
 (N'Valentina',N'Castro',    N'valentina@vga.com',     N'3510000016', @hash, N'valentina', 1),
 (N'Camila',   N'Vega',      N'camila@vga.com',        N'3510000017', @hash, N'camila', 1);

DECLARE @uDuenio INT = (SELECT ID_Usuario FROM dbo.Usuarios WHERE Mail=N'duenio@vga.com');
DECLARE @uAdmin  INT = (SELECT ID_Usuario FROM dbo.Usuarios WHERE Mail=N'admin@vga.com');
DECLARE @uRecep  INT = (SELECT ID_Usuario FROM dbo.Usuarios WHERE Mail=N'recepcion@vga.com');
DECLARE @uEntre  INT = (SELECT ID_Usuario FROM dbo.Usuarios WHERE Mail=N'entrenador@vga.com');
DECLARE @uNutri  INT = (SELECT ID_Usuario FROM dbo.Usuarios WHERE Mail=N'nutricionista@vga.com');

/* ====================== ORGANIZACIÓN + SUCURSALES ====================== */
INSERT dbo.Organizaciones (Nombre, RazonSocial, CUIT, ID_Duenio)
VALUES (N'VirtualGym', N'VirtualGym S.R.L.', N'30-12345678-9', @uDuenio);
DECLARE @org INT = SCOPE_IDENTITY();

INSERT dbo.Gimnasios (ID_Organizacion, Nombre, Direccion, Latitud, Longitud, Telefono, Email, EsCasaMatriz) VALUES
 (@org, N'Casa Central',   N'Av. Colón 1234, Córdoba',          -31.408571, -64.199845, N'351-4000000', N'central@vga.com', 1),
 (@org, N'Sucursal Norte', N'Av. Rafael Núñez 4567, Córdoba',   -31.364694, -64.235999, N'351-4111111', N'norte@vga.com',   0),
 (@org, N'Sucursal Sur',   N'Av. Vélez Sarsfield 900, Córdoba', -31.429559, -64.192753, N'351-4222222', N'sur@vga.com',     0);

DECLARE @gCentral INT = (SELECT ID_Gimnasio FROM dbo.Gimnasios WHERE ID_Organizacion=@org AND Nombre=N'Casa Central');
DECLARE @gNorte   INT = (SELECT ID_Gimnasio FROM dbo.Gimnasios WHERE ID_Organizacion=@org AND Nombre=N'Sucursal Norte');
DECLARE @gSur     INT = (SELECT ID_Gimnasio FROM dbo.Gimnasios WHERE ID_Organizacion=@org AND Nombre=N'Sucursal Sur');

/* ========================= ROLES POR SUCURSAL ========================= */
DECLARE @rAdmin INT = (SELECT ID_Rol FROM dbo.Roles WHERE Nombre=N'Administrador');
DECLARE @rRecep INT = (SELECT ID_Rol FROM dbo.Roles WHERE Nombre=N'Recepcionista');
DECLARE @rEntre INT = (SELECT ID_Rol FROM dbo.Roles WHERE Nombre=N'Entrenador');
DECLARE @rNutri INT = (SELECT ID_Rol FROM dbo.Roles WHERE Nombre=N'Nutricionista');
DECLARE @rSocio INT = (SELECT ID_Rol FROM dbo.Roles WHERE Nombre=N'Socio');

INSERT dbo.UsuarioGimnasioRol (ID_Usuario, ID_Gimnasio, ID_Rol) VALUES
 (@uAdmin, @gCentral, @rAdmin), (@uAdmin, @gNorte, @rAdmin), (@uAdmin, @gSur, @rAdmin),
 (@uRecep, @gCentral, @rRecep),
 (@uEntre, @gCentral, @rEntre), (@uEntre, @gNorte, @rEntre),
 (@uNutri, @gCentral, @rNutri);

/* ============================== SOCIOS ============================== */
DECLARE @estActivo INT = (SELECT ID_Estado FROM dbo.SocioEstados WHERE Nombre=N'Activo');
DECLARE @estMoroso INT = (SELECT ID_Estado FROM dbo.SocioEstados WHERE Nombre=N'Moroso');

DECLARE @objMasa INT = (SELECT ID_Objetivo FROM dbo.Objetivos WHERE Nombre=N'Ganar masa muscular');
DECLARE @objGrasa INT = (SELECT ID_Objetivo FROM dbo.Objetivos WHERE Nombre=N'Perder grasa');
DECLARE @objMant INT = (SELECT ID_Objetivo FROM dbo.Objetivos WHERE Nombre=N'Mantenimiento');
DECLARE @objFuerza INT = (SELECT ID_Objetivo FROM dbo.Objetivos WHERE Nombre=N'Fuerza');

-- Socios de Casa Central
;WITH sc AS (
  SELECT u.ID_Usuario, ROW_NUMBER() OVER (ORDER BY u.ID_Usuario) rn
  FROM dbo.Usuarios u
  WHERE u.Mail IN (N'socio@vga.com', N'lucia@vga.com', N'martin@vga.com',
                   N'julieta@vga.com', N'nicolas@vga.com', N'tomas@vga.com')
)
INSERT dbo.Socios (ID_Usuario, ID_Gimnasio, NumeroSocio, ID_Estado, ID_Objetivo, FechaNacimiento, FechaAlta)
SELECT sc.ID_Usuario, @gCentral, sc.rn,
       CASE WHEN sc.rn = 6 THEN @estMoroso ELSE @estActivo END,
       CASE sc.rn % 4 WHEN 0 THEN @objMasa WHEN 1 THEN @objGrasa WHEN 2 THEN @objMant ELSE @objFuerza END,
       DATEADD(YEAR, -20 - sc.rn, @hoy),
       DATEADD(DAY, -40 - sc.rn * 12, @hoy)
FROM sc;

-- Socios de Sucursal Norte
;WITH sn AS (
  SELECT u.ID_Usuario, ROW_NUMBER() OVER (ORDER BY u.ID_Usuario) rn
  FROM dbo.Usuarios u
  WHERE u.Mail IN (N'valentina@vga.com', N'camila@vga.com')
)
INSERT dbo.Socios (ID_Usuario, ID_Gimnasio, NumeroSocio, ID_Estado, ID_Objetivo, FechaNacimiento, FechaAlta)
SELECT sn.ID_Usuario, @gNorte, sn.rn, @estActivo,
       CASE sn.rn % 2 WHEN 0 THEN @objGrasa ELSE @objMant END,
       DATEADD(YEAR, -25 - sn.rn, @hoy),
       DATEADD(DAY, -50 - sn.rn * 10, @hoy)
FROM sn;

-- Rol "Socio" por sucursal para cada socio
INSERT dbo.UsuarioGimnasioRol (ID_Usuario, ID_Gimnasio, ID_Rol)
SELECT s.ID_Usuario, s.ID_Gimnasio, @rSocio FROM dbo.Socios s;

/* ============================== PLANES ============================== */
INSERT dbo.Planes (ID_Organizacion, Nombre, Descripcion, Precio, DuracionDias, Nivel, AccesoMultiSede) VALUES
 (@org, N'Full',     N'Acceso libre a todas las sucursales', 25000, 30, N'avanzado',     1),
 (@org, N'Estándar', N'Acceso a tu sucursal de origen',      18000, 30, N'intermedio',   0),
 (@org, N'Básico',   N'3 días por semana',                   12000, 30, N'principiante', 0);

DECLARE @planFull INT = (SELECT ID_Plan FROM dbo.Planes WHERE ID_Organizacion=@org AND Nombre=N'Full');
DECLARE @planStd  INT = (SELECT ID_Plan FROM dbo.Planes WHERE ID_Organizacion=@org AND Nombre=N'Estándar');
DECLARE @planBas  INT = (SELECT ID_Plan FROM dbo.Planes WHERE ID_Organizacion=@org AND Nombre=N'Básico');

/* =========================== MEMBRESÍAS =========================== */
INSERT dbo.Membresias (ID_Socio, ID_Plan, ID_Gimnasio, FechaInicio, PrecioAcordado, Activa)
SELECT s.ID_Socio,
       CASE s.ID_Socio % 3 WHEN 0 THEN @planFull WHEN 1 THEN @planStd ELSE @planBas END,
       s.ID_Gimnasio,
       DATEADD(MONTH, -4, @hoy),
       p.Precio, 1
FROM dbo.Socios s
JOIN dbo.Planes p ON p.ID_Plan = CASE s.ID_Socio % 3 WHEN 0 THEN @planFull WHEN 1 THEN @planStd ELSE @planBas END;

/* ============================== PAGOS ============================== */
DECLARE @estPagado INT = (SELECT ID_EstadoPago FROM dbo.PagoEstados WHERE Nombre=N'Pagado');
DECLARE @estPend   INT = (SELECT ID_EstadoPago FROM dbo.PagoEstados WHERE Nombre=N'Pendiente');
DECLARE @fEfectivo INT = (SELECT ID_FormaPago FROM dbo.FormasPago WHERE Nombre=N'Efectivo');
DECLARE @fMP       INT = (SELECT ID_FormaPago FROM dbo.FormasPago WHERE Nombre=N'Mercado Pago');

;WITH meses AS (
  SELECT n FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12)) m(n)
  WHERE n <= @mes
)
INSERT dbo.Pagos (ID_Socio, ID_Gimnasio, ID_Membresia, ID_Plan, ID_FormaPago,
                  PeriodoAnio, PeriodoMes, Monto, ID_EstadoPago, FechaPago)
SELECT s.ID_Socio, s.ID_Gimnasio, m.ID_Membresia, m.ID_Plan,
       CASE WHEN meses.n % 2 = 0 THEN @fMP ELSE @fEfectivo END,
       @anio, meses.n, m.PrecioAcordado,
       CASE
         WHEN s.ID_Estado = @estMoroso AND meses.n >= @mes - 1 THEN @estPend
         WHEN meses.n < @mes THEN @estPagado
         ELSE @estPend
       END,
       CASE WHEN meses.n < @mes AND NOT (s.ID_Estado = @estMoroso AND meses.n >= @mes - 1)
            THEN DATEADD(DAY, 2, DATEFROMPARTS(@anio, meses.n, 1)) END
FROM dbo.Socios s
JOIN dbo.Membresias m ON m.ID_Socio = s.ID_Socio AND m.Activa = 1
CROSS JOIN meses;

/* ============================ ASISTENCIAS ============================ */
-- Asistencias del mes en curso, hasta hoy, ~60% de días por socio
;WITH dias AS (
  SELECT TOP (DAY(@hoy)) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS d
  FROM sys.all_objects
)
INSERT dbo.Asistencias (ID_Socio, ID_Gimnasio, FechaHoraEntrada)
SELECT s.ID_Socio, s.ID_Gimnasio,
       DATEADD(HOUR, 8 + ((CHECKSUM(s.ID_Socio, dias.d) & 15) % 13),
               CAST(DATEFROMPARTS(@anio, @mes, dias.d) AS datetime2(0)))
FROM dbo.Socios s
CROSS JOIN dias
WHERE (CHECKSUM(s.ID_Socio, dias.d) & 7) < 5           -- ~60% de asistencia
  AND DATEFROMPARTS(@anio, @mes, dias.d) <= @hoy;

/* ============================= EJERCICIOS ============================= */
INSERT dbo.Ejercicios (Nombre, Descripcion, ID_Grupo)
SELECT v.Nombre, v.Descr, g.ID_Grupo
FROM (VALUES
  (N'Press banca',              N'Pecho con barra, banco plano',   N'Pecho'),
  (N'Press inclinado',          N'Pecho superior con mancuernas',  N'Pecho'),
  (N'Aperturas',                N'Pecho con mancuernas',           N'Pecho'),
  (N'Fondos en paralelas',      N'Pecho y tríceps, peso corporal', N'Pecho'),
  (N'Dominadas',                N'Espalda, peso corporal',         N'Espalda'),
  (N'Remo con barra',           N'Espalda media',                  N'Espalda'),
  (N'Remo en polea',            N'Espalda con polea baja',         N'Espalda'),
  (N'Peso muerto',              N'Cadena posterior completa',      N'Espalda'),
  (N'Sentadilla',               N'Piernas con barra',              N'Piernas'),
  (N'Prensa',                   N'Piernas en máquina',             N'Piernas'),
  (N'Extensión de cuádriceps',  N'Aislamiento de cuádriceps',      N'Piernas'),
  (N'Curl femoral',             N'Aislamiento de isquios',         N'Piernas'),
  (N'Elevación de gemelos',     N'Pantorrillas',                   N'Piernas'),
  (N'Hip thrust',               N'Glúteos con barra',              N'Glúteos'),
  (N'Peso muerto rumano',       N'Glúteos e isquios',              N'Glúteos'),
  (N'Press militar',            N'Hombros con barra',              N'Hombros'),
  (N'Elevaciones laterales',    N'Deltoides lateral',              N'Hombros'),
  (N'Curl con barra',           N'Bíceps con barra recta',         N'Bíceps'),
  (N'Curl martillo',            N'Bíceps y braquial',              N'Bíceps'),
  (N'Extensión en polea',       N'Tríceps con soga',               N'Tríceps'),
  (N'Press francés',            N'Tríceps con barra Z',            N'Tríceps'),
  (N'Plancha',                  N'Core isométrico',                N'Core'),
  (N'Elevación de piernas',     N'Abdomen inferior',               N'Core'),
  (N'Crunch',                   N'Abdomen superior',               N'Core')
) v(Nombre, Descr, Grupo)
JOIN dbo.GruposMusculares g ON g.Nombre = v.Grupo;

/* ============================== ALIMENTOS ============================== */
INSERT dbo.Alimentos (Nombre, Calorias, Proteinas, Carbohidratos, Grasas, ID_Categoria, ID_Objetivo)
SELECT v.Nombre, v.Cal, v.Prot, v.Carb, v.Gras, c.ID_Categoria, o.ID_Objetivo
FROM (VALUES
  (N'Pechuga de pollo', 165, 31,  0,  4, N'Proteínas',     N'Ganar masa muscular'),
  (N'Carne magra',      175, 26,  0,  8, N'Proteínas',     N'Ganar masa muscular'),
  (N'Atún al natural',  116, 26,  0,  1, N'Proteínas',     N'Perder grasa'),
  (N'Huevo',             78,  6,  1,  5, N'Proteínas',     N'Mantenimiento'),
  (N'Arroz integral',   111,  3, 23,  1, N'Carbohidratos', N'Ganar masa muscular'),
  (N'Fideos integrales',124,  5, 25,  1, N'Carbohidratos', N'Ganar masa muscular'),
  (N'Avena',            389, 17, 66,  7, N'Carbohidratos', N'Mantenimiento'),
  (N'Batata',            86,  2, 20,  0, N'Carbohidratos', N'Perder grasa'),
  (N'Palta',            160,  2,  9, 15, N'Grasas',        N'Mantenimiento'),
  (N'Frutos secos',     607, 20, 21, 54, N'Grasas',        N'Ganar masa muscular'),
  (N'Aceite de oliva',  884,  0,  0,100, N'Grasas',        N'Mantenimiento'),
  (N'Ensalada mixta',    35,  2,  6,  0, N'Verduras',      N'Perder grasa'),
  (N'Brócoli',           34,  3,  7,  0, N'Verduras',      N'Perder grasa'),
  (N'Manzana',           52,  0, 14,  0, N'Frutas',        N'Perder grasa'),
  (N'Banana',            89,  1, 23,  0, N'Frutas',        N'Ganar masa muscular'),
  (N'Yogur descremado',  59, 10,  4,  0, N'Lácteos',       N'Perder grasa'),
  (N'Queso port salut', 300, 20,  2, 24, N'Lácteos',       N'Ganar masa muscular'),
  (N'Lentejas',         116,  9, 20,  0, N'Legumbres',     N'Mantenimiento'),
  (N'Garbanzos',        164,  9, 27,  3, N'Legumbres',     N'Mantenimiento')
) v(Nombre, Cal, Prot, Carb, Gras, Categoria, Objetivo)
JOIN dbo.AlimentosCategorias c ON c.Nombre = v.Categoria
JOIN dbo.Objetivos o           ON o.Nombre = v.Objetivo;

/* ======================= RUTINAS (plantillas) ======================= */
INSERT dbo.Rutinas (ID_Organizacion, Nombre, Descripcion, Intensidad, DiasPorSemana, EjerciciosPorDia, ID_Objetivo) VALUES
 (@org, N'Full Body 3 días', N'Cuerpo completo, ideal para empezar', N'Media', 3, 5, @objMant),
 (@org, N'Push Pull Legs',   N'4 días divididos por patrón',         N'Alta',  4, 6, @objMasa),
 (@org, N'Definición',       N'Circuito de 5 días',                  N'Alta',  5, 5, @objGrasa);

DECLARE @rutFB INT = (SELECT ID_Rutina FROM dbo.Rutinas WHERE ID_Organizacion=@org AND Nombre=N'Full Body 3 días');

INSERT dbo.RutinaDetalle (ID_Rutina, Dia, Orden, ID_Ejercicio, Series, Repeticiones, DescansoSeg)
SELECT @rutFB, v.Dia, v.Orden, e.ID_Ejercicio, v.Series, v.Reps, v.Descanso
FROM (VALUES
  (1,1,N'Sentadilla',3,N'8-10',120),
  (1,2,N'Press banca',3,N'8-10',120),
  (1,3,N'Remo con barra',3,N'8-10',90),
  (1,4,N'Press militar',3,N'10-12',90),
  (1,5,N'Plancha',3,N'40 s',60),
  (2,1,N'Peso muerto',3,N'6-8',150),
  (2,2,N'Dominadas',3,N'al fallo',120),
  (2,3,N'Press inclinado',3,N'8-10',90),
  (2,4,N'Curl con barra',3,N'10-12',60),
  (2,5,N'Elevación de piernas',3,N'12-15',60),
  (3,1,N'Prensa',4,N'10-12',90),
  (3,2,N'Hip thrust',3,N'10-12',90),
  (3,3,N'Aperturas',3,N'12-15',60),
  (3,4,N'Extensión en polea',3,N'12-15',60),
  (3,5,N'Crunch',3,N'15-20',45)
) v(Dia, Orden, Ejercicio, Series, Reps, Descanso)
JOIN dbo.Ejercicios e ON e.Nombre = v.Ejercicio;

/* ================= RUTINA / DIETA ASIGNADAS A UN SOCIO ================= */
DECLARE @sSocio INT = (SELECT s.ID_Socio FROM dbo.Socios s JOIN dbo.Usuarios u ON u.ID_Usuario=s.ID_Usuario WHERE u.Mail=N'socio@vga.com');

INSERT dbo.SocioRutinas (ID_Socio, ID_Rutina, ID_Entrenador, Nombre, FechaInicio, Activa)
VALUES (@sSocio, @rutFB, @uEntre, N'Full Body 3 días', DATEADD(DAY, -20, @hoy), 1);
DECLARE @sr INT = SCOPE_IDENTITY();

INSERT dbo.SocioRutinaDetalle (ID_SocioRutina, Dia, Orden, ID_Ejercicio)
SELECT @sr, rd.Dia, rd.Orden, rd.ID_Ejercicio
FROM dbo.RutinaDetalle rd WHERE rd.ID_Rutina = @rutFB;

INSERT dbo.SocioDietas (ID_Socio, ID_Nutricionista, Nombre, ID_Objetivo, FechaInicio, Activa)
VALUES (@sSocio, @uNutri, N'Plan mantenimiento', @objMant, DATEADD(DAY, -15, @hoy), 1);
DECLARE @sd INT = SCOPE_IDENTITY();

INSERT dbo.SocioDietaDetalle (ID_SocioDieta, Dia, Categoria, Alimento) VALUES
 (@sd, N'Día 1', N'Desayuno', N'Avena con banana'),
 (@sd, N'Día 1', N'Almuerzo', N'Pechuga de pollo con arroz integral'),
 (@sd, N'Día 1', N'Merienda', N'Yogur descremado con frutos secos'),
 (@sd, N'Día 1', N'Cena',     N'Carne magra con ensalada mixta'),
 (@sd, N'Día 2', N'Desayuno', N'Huevos revueltos con pan integral'),
 (@sd, N'Día 2', N'Almuerzo', N'Atún con fideos integrales'),
 (@sd, N'Día 2', N'Merienda', N'Manzana y queso port salut'),
 (@sd, N'Día 2', N'Cena',     N'Lentejas con verduras'),
 (@sd, N'Día 3', N'Desayuno', N'Yogur con avena'),
 (@sd, N'Día 3', N'Almuerzo', N'Pollo con batata al horno'),
 (@sd, N'Día 3', N'Merienda', N'Banana con frutos secos'),
 (@sd, N'Día 3', N'Cena',     N'Tortilla de brócoli');

/* ============================ MEDICIONES ============================ */
-- k = 0 es la medición de hoy; k = 2 es de hace ~8 semanas (más peso y grasa)
INSERT dbo.Mediciones (ID_Socio, Fecha, Peso, GrasaPct, MusculoPct, Cintura, ID_RegistradoPor)
SELECT s.ID_Socio, DATEADD(WEEK, w.k * -4, @hoy),
       82.0 + (w.k * 1.2),
       22.0 + (w.k * 0.8),
       38.0 - (w.k * 0.5),
       92.0 + (w.k * 1.5),
       @uEntre
FROM dbo.Socios s
JOIN dbo.Usuarios u ON u.ID_Usuario = s.ID_Usuario
CROSS JOIN (VALUES (0),(1),(2)) w(k)
WHERE u.Mail IN (N'socio@vga.com', N'lucia@vga.com');

/* ============================== HORARIOS ============================== */
INSERT dbo.Horarios (ID_Gimnasio, DiaSemana, HoraApertura, HoraCierre, Orden)
SELECT g.ID_Gimnasio, v.Dia, v.Ap, v.Ci, v.Dia
FROM (VALUES
  (1,'07:00','23:00'),(2,'07:00','23:00'),(3,'07:00','23:00'),
  (4,'07:00','23:00'),(5,'07:00','23:00'),(6,'09:00','15:00')
) v(Dia, Ap, Ci)
CROSS JOIN dbo.Gimnasios g
WHERE g.ID_Organizacion = @org;

/* ============================ CONTENIDO WEB ============================ */
INSERT dbo.CarouselItems (ID_Organizacion, Tipo, Titulo, Descripcion, Orden) VALUES
 (@org, N'servicios', N'Musculación',      N'Sala equipada con máquinas de última generación', 1),
 (@org, N'servicios', N'Clases grupales',  N'Funcional, spinning, zumba y más',                2),
 (@org, N'servicios', N'Asesoría nutricional', N'Plan de alimentación a medida',               3),
 (@org, N'frases',    N'Superá tus límites', N'El único mal entrenamiento es el que no hiciste', 1),
 (@org, N'frases',    N'Constancia',        N'Los resultados llegan a quien no abandona',       2);

INSERT dbo.FooterConfig (ID_Organizacion, Seccion, Clave, Valor, Orden) VALUES
 (@org, N'Contacto', N'telefono',  N'351-4000000',              1),
 (@org, N'Contacto', N'email',     N'info@virtualgym.com',      2),
 (@org, N'Contacto', N'direccion', N'Av. Colón 1234, Córdoba',  3),
 (@org, N'Redes',    N'instagram', N'@virtualgym',              1),
 (@org, N'Redes',    N'facebook',  N'/virtualgym',              2),
 (@org, N'Info',     N'horario',   N'Lun a Vie 7 a 23 · Sáb 9 a 15', 1);

COMMIT TRANSACTION;

PRINT '=====================================================';
PRINT ' Datos demo cargados.  Contraseña de todos: 123456';
PRINT '   duenio@vga.com          (Dueño)';
PRINT '   admin@vga.com           (Administrador)';
PRINT '   recepcion@vga.com       (Recepcionista)';
PRINT '   entrenador@vga.com      (Entrenador)';
PRINT '   nutricionista@vga.com   (Nutricionista)';
PRINT '   socio@vga.com + otros 7 socios';
PRINT '=====================================================';
GO
