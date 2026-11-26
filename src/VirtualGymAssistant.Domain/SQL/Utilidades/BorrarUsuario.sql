/* =====================================================================
   BORRAR UN USUARIO Y TODO LO RELACIONADO CON ÉL
   ---------------------------------------------------------------------
   Cómo se usa:
     1. Poné en @Usuario (más abajo) el email, el nombre de usuario o el
        ID de la persona.
     2. Ejecutá el script entero (F5 en SSMS).

   Si querés ver antes qué se borraría, poné @SoloMostrar = 1: hace todo
   igual, te muestra el resumen y lo deshace al final.

   Qué borra:
     - Sus fichas de socio (de todas las sucursales) y todo lo que cuelga
       de ellas: asistencias, pagos, membresías, mediciones, rutinas y
       dietas, y los accesos a otras sucursales.
     - Sus publicaciones del foro y las respuestas que tenían, sean de
       quien sean (una respuesta no tiene sentido sin la publicación).
     - Sus roles en las sucursales, sus códigos de verificación y de
       recuperación de clave y, por último, la cuenta.

   Qué conserva (porque es de otras personas):
     - Las mediciones, rutinas y dietas que esta persona le cargó a OTROS
       socios (como entrenador o nutricionista). Siguen estando; sólo se
       les quita su nombre como autor.

   Cuándo se niega a borrar (y no toca nada):
     - Si no existe, o si el texto coincide con más de un usuario.
     - Si es dueño de una organización: borrarlo implicaría borrar la
       organización entera (sucursales, planes, pagos...) y este script no
       llega tan lejos.

   Es todo o nada: va dentro de una transacción y, si algo falla, se
   revierte completo. Si algún día se agrega una tabla nueva que apunta a
   Usuarios o a Socios, SQL Server frena el borrado con un error que
   nombra esa tabla, y no queda nada a medias.

   Un detalle que SQL no puede resolver: las imágenes o PDF que se subieron
   al foro son archivos de la carpeta Uploads del API. El script no los
   borra; al final te lista cuáles quedaron para que los saques a mano.

   Si en vez de SSMS lo corrés con sqlcmd, agregá "-f 65001": sin eso las
   tildes de los mensajes se ven mal (no cambia lo que se borra).
   ===================================================================== */

USE [VirtualGymAssistant];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;           -- ante cualquier error se revierte todo
SET QUOTED_IDENTIFIER ON;    -- lo piden los índices filtrados de Socios y Membresías
SET ANSI_NULLS ON;

/* ---------------------------------------------------------------------
   >>> LO ÚNICO QUE HAY QUE TOCAR <<<
   --------------------------------------------------------------------- */
DECLARE @Usuario     NVARCHAR(150) = N'';   -- email, nombre de usuario o ID. Ej.: N'lucia@vga.com'
DECLARE @SoloMostrar BIT           = 0;     -- 1 = simulacro: muestra qué se borraría y lo deshace

/* ---------------------------------------------------------------------
   1. Quién es
   --------------------------------------------------------------------- */
DECLARE @IdUsuario     INT;
DECLARE @Coincidencias INT;
DECLARE @Quien         NVARCHAR(500);

SET @Usuario = LTRIM(RTRIM(@Usuario));

SELECT @Coincidencias = COUNT(*)
FROM dbo.Usuarios
WHERE Mail = @Usuario
   OR Username = @Usuario
   OR ID_Usuario = TRY_CONVERT(INT, @Usuario);

IF @Coincidencias = 0
BEGIN
    PRINT N'No se borró nada: no existe ningún usuario con email, nombre de usuario o ID "' + @Usuario + N'".';
    RETURN;
END

IF @Coincidencias > 1
BEGIN
    PRINT N'No se borró nada: "' + @Usuario + N'" coincide con más de un usuario. Usá el email completo.';
    RETURN;
END

SELECT @IdUsuario = ID_Usuario,
       @Quien     = ISNULL(NULLIF(LTRIM(RTRIM(ISNULL(Nombre, N'') + N' ' + ISNULL(Apellido, N''))), N''), N'(sin nombre)')
                  + N' <' + Mail + N'>, usuario "' + Username + N'", ID ' + CAST(ID_Usuario AS NVARCHAR(12))
FROM dbo.Usuarios
WHERE Mail = @Usuario
   OR Username = @Usuario
   OR ID_Usuario = TRY_CONVERT(INT, @Usuario);

/* ---------------------------------------------------------------------
   2. Lo que no se puede borrar con este script
   --------------------------------------------------------------------- */
IF EXISTS (SELECT 1 FROM dbo.Organizaciones WHERE ID_Duenio = @IdUsuario)
BEGIN
    DECLARE @Organizaciones NVARCHAR(500) = N'';

    SELECT @Organizaciones += CASE WHEN @Organizaciones = N'' THEN N'' ELSE N', ' END + N'"' + Nombre + N'"'
    FROM dbo.Organizaciones
    WHERE ID_Duenio = @IdUsuario;

    PRINT N'No se borró nada: ' + @Quien + N' es el dueño de ' + @Organizaciones
        + N'. Borrarlo implicaría borrar la organización entera y este script no lo hace.';
    RETURN;
END

PRINT N'Usuario: ' + @Quien;

/* ---------------------------------------------------------------------
   3. Borrado (todo dentro de una transacción)
   --------------------------------------------------------------------- */
-- Registro de lo que se toca. Son variables de tabla a propósito: si la transacción se
-- deshace (simulacro), conservan su contenido y se puede mostrar igual el resumen.
DECLARE @Cambios TABLE (Orden INT IDENTITY(1,1), Que NVARCHAR(200));
DECLARE @Fichas  TABLE (ID_Socio INT PRIMARY KEY);
DECLARE @Posts   TABLE (ID_Post  INT PRIMARY KEY);
DECLARE @Archivos TABLE (Archivo NVARCHAR(260));

INSERT @Fichas (ID_Socio)
SELECT ID_Socio FROM dbo.Socios WHERE ID_Usuario = @IdUsuario;

-- Publicaciones del foro: las suyas y, en cadena, todas las respuestas que cuelgan de ellas.
INSERT @Posts (ID_Post)
SELECT ID_Post FROM dbo.ForoPosts WHERE ID_Usuario = @IdUsuario;

WHILE @@ROWCOUNT > 0
    INSERT @Posts (ID_Post)
    SELECT p.ID_Post
    FROM dbo.ForoPosts p
    WHERE p.ID_PostPadre IN (SELECT ID_Post FROM @Posts)
      AND p.ID_Post NOT IN (SELECT ID_Post FROM @Posts);

INSERT @Archivos (Archivo)
SELECT DISTINCT Archivo
FROM dbo.ForoPosts
WHERE ID_Post IN (SELECT ID_Post FROM @Posts)
  AND Archivo IS NOT NULL;

BEGIN TRANSACTION;

-- 3.1 Lo que cuelga de sus fichas de socio -----------------------------------------------
DELETE FROM dbo.Asistencias
OUTPUT N'Asistencias' INTO @Cambios (Que)
WHERE ID_Socio IN (SELECT ID_Socio FROM @Fichas);

-- Los pagos van antes que las membresías, porque Pagos.ID_Membresia las referencia.
DELETE FROM dbo.Pagos
OUTPUT N'Pagos' INTO @Cambios (Que)
WHERE ID_Socio IN (SELECT ID_Socio FROM @Fichas);

DELETE FROM dbo.Membresias
OUTPUT N'Membresías' INTO @Cambios (Que)
WHERE ID_Socio IN (SELECT ID_Socio FROM @Fichas);

DELETE FROM dbo.Mediciones
OUTPUT N'Mediciones propias' INTO @Cambios (Que)
WHERE ID_Socio IN (SELECT ID_Socio FROM @Fichas);

DELETE FROM dbo.SocioAccesoGimnasio
OUTPUT N'Accesos a otras sucursales' INTO @Cambios (Que)
WHERE ID_Socio IN (SELECT ID_Socio FROM @Fichas);

DELETE d
OUTPUT N'Ejercicios de sus rutinas' INTO @Cambios (Que)
FROM dbo.SocioRutinaDetalle d
JOIN dbo.SocioRutinas r ON r.ID_SocioRutina = d.ID_SocioRutina
WHERE r.ID_Socio IN (SELECT ID_Socio FROM @Fichas);

DELETE FROM dbo.SocioRutinas
OUTPUT N'Rutinas' INTO @Cambios (Que)
WHERE ID_Socio IN (SELECT ID_Socio FROM @Fichas);

DELETE d
OUTPUT N'Comidas de sus dietas' INTO @Cambios (Que)
FROM dbo.SocioDietaDetalle d
JOIN dbo.SocioDietas s ON s.ID_SocioDieta = d.ID_SocioDieta
WHERE s.ID_Socio IN (SELECT ID_Socio FROM @Fichas);

DELETE FROM dbo.SocioDietas
OUTPUT N'Dietas' INTO @Cambios (Que)
WHERE ID_Socio IN (SELECT ID_Socio FROM @Fichas);

DELETE FROM dbo.Socios
OUTPUT N'Fichas de socio' INTO @Cambios (Que)
WHERE ID_Usuario = @IdUsuario;

-- 3.2 Lo que cargó para otras personas: se conserva, sólo se le quita su nombre ---------
UPDATE dbo.Mediciones
SET ID_RegistradoPor = NULL
OUTPUT N'Mediciones que cargó a otros socios (se conservan, sin su nombre)' INTO @Cambios (Que)
WHERE ID_RegistradoPor = @IdUsuario;

UPDATE dbo.SocioRutinas
SET ID_Entrenador = NULL
OUTPUT N'Rutinas que asignó a otros socios (se conservan, sin su nombre)' INTO @Cambios (Que)
WHERE ID_Entrenador = @IdUsuario;

UPDATE dbo.SocioDietas
SET ID_Nutricionista = NULL
OUTPUT N'Dietas que asignó a otros socios (se conservan, sin su nombre)' INTO @Cambios (Que)
WHERE ID_Nutricionista = @IdUsuario;

-- 3.3 Foro ------------------------------------------------------------------------------
-- Un solo DELETE para toda la cadena: SQL Server revisa la clave foránea recién al final
-- de la sentencia, así que las respuestas y su publicación salen juntas.
DELETE FROM dbo.ForoPosts
OUTPUT N'Publicaciones del foro (con sus respuestas)' INTO @Cambios (Que)
WHERE ID_Post IN (SELECT ID_Post FROM @Posts);

-- 3.4 Accesos y cuenta ------------------------------------------------------------------
DELETE FROM dbo.UsuarioGimnasioRol
OUTPUT N'Roles en sucursales' INTO @Cambios (Que)
WHERE ID_Usuario = @IdUsuario;

DELETE FROM dbo.TokensVerificacionEmail
OUTPUT N'Códigos de verificación de email' INTO @Cambios (Que)
WHERE ID_Usuario = @IdUsuario;

DELETE FROM dbo.TokensRecuperacion
OUTPUT N'Códigos de recuperación de clave' INTO @Cambios (Que)
WHERE ID_Usuario = @IdUsuario;

DELETE FROM dbo.Usuarios
OUTPUT N'Cuenta de usuario' INTO @Cambios (Que)
WHERE ID_Usuario = @IdUsuario;

/* ---------------------------------------------------------------------
   4. Confirmar o deshacer, y mostrar qué pasó
   --------------------------------------------------------------------- */
IF @SoloMostrar = 1
BEGIN
    ROLLBACK TRANSACTION;
    PRINT N'SIMULACRO: no se borró nada. Esto es lo que se habría borrado (poné @SoloMostrar = 0 para borrar de verdad):';
END
ELSE
BEGIN
    COMMIT TRANSACTION;
    PRINT N'LISTO: se borró el usuario y todo lo relacionado con él:';
END

SELECT Que AS [Qué], COUNT(*) AS [Filas]
FROM @Cambios
GROUP BY Que
ORDER BY MIN(Orden);

IF EXISTS (SELECT 1 FROM @Archivos)
    SELECT Archivo AS [Archivos del foro que quedan en la carpeta Uploads del API (borralos a mano)]
    FROM @Archivos
    ORDER BY Archivo;
GO
