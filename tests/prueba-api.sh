#!/bin/bash
# =====================================================================
#  VirtualGymAssistant - Prueba de humo de la API
# ---------------------------------------------------------------------
#  Recorre los flujos principales y verifica el codigo HTTP de cada uno,
#  incluyendo los casos que DEBEN fallar (permisos, reglas de negocio).
#
#  Uso:
#     1. Levantar la API:   dotnet run --project src/VirtualGymAssistant.API
#     2. Correr el script:  bash tests/prueba-api.sh
#
#  Es idempotente: se puede correr varias veces seguidas sin limpiar la base.
# =====================================================================

BASE="${1:-http://localhost:5180}/api"
OK=0
FALLO=0
TMP=$(mktemp)

# probar <nombre> <codigos_esperados> <metodo> <url> <token> <gimnasio> <body>
# En <codigos_esperados> se puede poner mas de uno separados por coma ("200,400")
# para los casos cuyo resultado depende del estado previo de la base.
probar() {
  local nombre="$1" esperados="$2" metodo="$3" url="$4" token="$5" gym="$6" body="$7"

  local args=(-s -o "$TMP" -w "%{http_code}" -X "$metodo" "$BASE$url")
  [ -n "$token" ] && args+=(-H "Authorization: Bearer $token")
  [ -n "$gym" ] && args+=(-H "X-Gimnasio-Id: $gym")
  [ -n "$body" ] && args+=(-H "Content-Type: application/json" -d "$body")

  local codigo
  codigo=$(curl "${args[@]}")

  if [[ ",$esperados," == *",$codigo,"* ]]; then
    echo "  OK   [$codigo] $nombre"
    OK=$((OK+1))
  else
    echo "  FALLA[$codigo, esperaba $esperados] $nombre"
    echo "        $(head -c 220 "$TMP")"
    FALLO=$((FALLO+1))
  fi
}

# "-X utf8" es imprescindible en Windows: sin eso Python lee la respuesta (que es UTF-8) con la
# pagina de codigos del sistema y "Colón" pasa a ser "ColÃ³n". Los tests que re-guardan un
# registro con lo que acaban de leer (seccion 11) escribirian esa version danada en la base.
extraer() { python -X utf8 -c "import sys,json;d=json.load(sys.stdin);print($1)" 2>/dev/null; }

# El codigo de un solo uso ya no vuelve en la respuesta de la API (se manda solo por
# mail), asi que para probar el flujo completo sin revisar una casilla de verdad lo
# leemos directo de la base de desarrollo. Codigo() y Usado se guardan en texto plano,
# a proposito: son de un solo uso y vencen en minutos, no son la contrasena.
codigo_de_verificacion() {
  sqlcmd -S "PC_JOTA\SQL2017" -d VirtualGymAssistant -E -h -1 -W -Q \
    "SET NOCOUNT ON; SELECT TOP 1 t.Codigo FROM TokensVerificacionEmail t JOIN Usuarios u ON u.ID_Usuario=t.ID_Usuario WHERE u.Mail=N'$1' AND t.Usado=0 ORDER BY t.FechaCreacion DESC, t.ID_Token DESC;" \
    2>/dev/null | tr -d '[:space:]'
}

# El usuario descartable de la seccion 10 se borra al terminar, para que cada corrida deje
# la base como la encontro: si no, se acumulan socios "Prueba Verificacion" en el padron.
# Borra solo ese correo exacto; si algo lo referencia, la transaccion se revierte y queda.
limpiar_usuario_de_prueba() {
  sqlcmd -S "PC_JOTA\SQL2017" -d VirtualGymAssistant -E -Q \
    "SET NOCOUNT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; DECLARE @u INT = (SELECT ID_Usuario FROM Usuarios WHERE Mail=N'$1'); IF @u IS NOT NULL BEGIN BEGIN TRY BEGIN TRAN; DELETE FROM TokensVerificacionEmail WHERE ID_Usuario=@u; DELETE FROM Socios WHERE ID_Usuario=@u; DELETE FROM UsuarioGimnasioRol WHERE ID_Usuario=@u; DELETE FROM Usuarios WHERE ID_Usuario=@u; COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; END CATCH END" \
    >/dev/null 2>&1
}

# Pone o saca la marca de "email verificado" de la cuenta de prueba (0 o 1). Sirve para
# simular una sesion vieja: una emitida cuando el registro todavia dejaba entrar sin
# confirmar el email, y que por eso sigue circulando con un token valido.
marcar_email_verificado() {
  # QUOTED_IDENTIFIER y ANSI_NULLS en ON: Usuarios tiene un indice filtrado (telefono unico) y
  # sin esos SET la base rechaza cualquier UPDATE. -b hace que sqlcmd devuelva error si falla.
  sqlcmd -b -S "PC_JOTA\SQL2017" -d VirtualGymAssistant -E -Q \
    "SET NOCOUNT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; UPDATE Usuarios SET EmailVerificado=$2 WHERE Mail=N'$1';" \
    >/dev/null 2>&1 || echo "  AVISO: no se pudo cambiar EmailVerificado en la base (fallo sqlcmd)"
}

echo "=============================================="
echo " 1. PUBLICO (sin autenticacion)"
echo "=============================================="
probar "Sucursales publicas"        200 GET  "/sucursales/publicas"         "" "" ""
# Las sedes demo vienen con su ubicacion cargada: con eso el sitio publico dibuja el mapa.
# (Una sucursal nueva puede no tenerla: es opcional, por eso se miran solo las tres de la demo.)
SIN_UBICACION=$(extraer "len([s for s in d if s['nombre'] in ('Casa Central','Sucursal Norte','Sucursal Sur') and (s.get('latitud') is None or s.get('longitud') is None)])" < "$TMP")
if [ "$SIN_UBICACION" = "0" ]; then
  echo "  OK   [200] Las sucursales demo traen latitud y longitud"
  OK=$((OK+1))
else
  echo "  FALLA[$SIN_UBICACION sin ubicacion] Faltan coordenadas en las sucursales demo (correr 01 y 02 de SQL)"
  FALLO=$((FALLO+1))
fi
probar "Planes publicos"            200 GET  "/planes/publicos"             "" "" ""
probar "Planes de una sucursal"     200 GET  "/planes/publicos?idGimnasio=1" "" "" ""
probar "Horarios publicos"          200 GET  "/horarios/publicos/1"         "" "" ""
probar "Carousel publico"           200 GET  "/sitio-web/carousel/publico"  "" "" ""
probar "Footer publico"             200 GET  "/sitio-web/footer/publico"    "" "" ""
probar "Padron sin token"           401 GET  "/socios"                      "" "" ""

echo ""
echo "=============================================="
echo " 2. AUTENTICACION"
echo "=============================================="
probar "Login admin"                200 POST "/auth/login" "" "" '{"email":"admin@vga.com","contrasena":"admin"}'
ADMIN=$(extraer "d['token']" < "$TMP")

probar "Login socio"                200 POST "/auth/login" "" "" '{"email":"socio@vga.com","contrasena":"123456"}'
SOCIO=$(extraer "d['token']" < "$TMP")

probar "Login recepcion"            200 POST "/auth/login" "" "" '{"email":"recepcion@vga.com","contrasena":"123456"}'
RECEP=$(extraer "d['token']" < "$TMP")

probar "Login entrenador"           200 POST "/auth/login" "" "" '{"email":"entrenador@vga.com","contrasena":"123456"}'
ENTRE=$(extraer "d['token']" < "$TMP")

probar "Login nutricionista"        200 POST "/auth/login" "" "" '{"email":"nutricionista@vga.com","contrasena":"123456"}'
NUTRI=$(extraer "d['token']" < "$TMP")

probar "Login duenio"               200 POST "/auth/login" "" "" '{"email":"duenio@vga.com","contrasena":"123456"}'
DUENIO=$(extraer "d['token']" < "$TMP")

probar "Login clave incorrecta"     401 POST "/auth/login" "" "" '{"email":"admin@vga.com","contrasena":"nopenope"}'
probar "Login email invalido"       400 POST "/auth/login" "" "" '{"email":"no-es-un-mail","contrasena":"x"}'
probar "Sesion actual (me)"         200 GET  "/auth/me" "$ADMIN" "" ""

echo ""
echo "=============================================="
echo " 3. ORGANIZACION Y SUCURSALES"
echo "=============================================="
probar "Organizaciones del duenio"  200 GET  "/organizaciones"        "$DUENIO" "" ""
probar "Sucursales visibles"        200 GET  "/sucursales"            "$ADMIN"  "1" ""
probar "Detalle de sucursal"        200 GET  "/sucursales/1"          "$ADMIN"  "1" ""
probar "Zonas"                      200 GET  "/sucursales/zonas"      "$ADMIN"  "1" ""
probar "Socio no crea sucursal"     403 POST "/sucursales"            "$SOCIO"  "1" '{"nombre":"Pirata","direccion":"Calle falsa 123"}'
probar "Admin no crea sucursal"     403 POST "/sucursales"            "$ADMIN"  "1" '{"nombre":"Nueva","direccion":"Av. Siempreviva 742"}'

echo ""
echo "=============================================="
echo " 4. PERSONAL, SOCIOS Y PLANES"
echo "=============================================="
probar "Roles"                      200 GET  "/usuarios/roles"         "$ADMIN" "1" ""
probar "Personal de la sucursal"    200 GET  "/usuarios"               "$ADMIN" "1" ""
probar "Socio no ve el personal"    403 GET  "/usuarios"               "$SOCIO" "1" ""
probar "Padron de socios"           200 GET  "/socios?tamanioPagina=5" "$RECEP" "1" ""
probar "Padron filtrado por texto"  200 GET  "/socios?texto=luc"       "$RECEP" "1" ""
probar "Estados de socio"           200 GET  "/socios/estados"         "$RECEP" "1" ""
probar "Mi ficha de socio"          200 GET  "/socios/mi-ficha"        "$SOCIO" "1" ""
probar "Planes"                     200 GET  "/planes"                 "$SOCIO" "1" ""
probar "Formas de pago"             200 GET  "/formas-de-pago"         "$SOCIO" "1" ""
probar "Socio no crea plan"         403 POST "/planes"                 "$SOCIO" "1" '{"nombre":"Trucho","precio":1}'

echo ""
echo "=============================================="
echo " 5. PAGOS"
echo "=============================================="
probar "Mis pagos"                  200 GET  "/pagos/mios"            "$SOCIO" "1" ""
probar "Pendientes (recepcion)"     200 GET  "/pagos/pendientes"      "$RECEP" "1" ""
probar "Socio no ve pendientes"     403 GET  "/pagos/pendientes"      "$SOCIO" "1" ""
probar "Grilla anual"               200 GET  "/pagos/grilla-anual"    "$RECEP" "1" ""
probar "Resumen de cobranza"        200 GET  "/pagos/resumen"         "$RECEP" "1" ""
probar "Listado con filtros"        200 GET  "/pagos?anio=2026&mes=9" "$RECEP" "1" ""
probar "Pago con plan inexistente"  404 POST "/pagos"                 "$SOCIO" "1" '{"mes":12,"anio":2026,"idPlan":9999,"idFormaPago":1}'
probar "Pago muy adelantado"        400 POST "/pagos"                 "$SOCIO" "1" '{"mes":12,"anio":2030,"idPlan":1,"idFormaPago":1}'

# Diciembre se usa como mes de prueba: si quedo Anulado de una corrida anterior,
# el sistema permite reintentarlo, asi que el resultado es 201 en ambos casos.
probar "Pagar diciembre"            201 POST "/pagos"                 "$SOCIO" "1" '{"mes":12,"anio":2026,"idPlan":1,"idFormaPago":1}'
PAGO=$(extraer "d['id']" < "$TMP")
probar "Repetir el mismo mes"       400 POST "/pagos"                 "$SOCIO" "1" '{"mes":12,"anio":2026,"idPlan":1,"idFormaPago":1}'
probar "Socio no aprueba su pago"   403 PUT  "/pagos/$PAGO/estado"    "$SOCIO" "1" '{"estado":"Pagado"}'
probar "Recepcion aprueba el pago"  200 PUT  "/pagos/$PAGO/estado"    "$RECEP" "1" '{"estado":"Pagado"}'
probar "Transicion invalida"        400 PUT  "/pagos/$PAGO/estado"    "$RECEP" "1" '{"estado":"Pendiente"}'
probar "Anular el pago"             200 PUT  "/pagos/$PAGO/estado"    "$RECEP" "1" '{"estado":"Anulado","observaciones":"Prueba automatica"}'
probar "Membresia del socio"        200 GET  "/membresias/socio/1"    "$RECEP" "1" ""

echo ""
echo "=============================================="
echo " 6. ASISTENCIAS"
echo "=============================================="
probar "Mis asistencias"            200 GET  "/asistencias/mias"           "$SOCIO" "1" ""
probar "Grilla mensual"             200 GET  "/asistencias/grilla-mensual" "$RECEP" "1" ""
probar "Resumen"                    200 GET  "/asistencias/resumen"        "$RECEP" "1" ""
probar "Socio no ve la grilla"      403 GET  "/asistencias/grilla-mensual" "$SOCIO" "1" ""

# La primera marcada del dia crea (201) o falla si ya habia una (400).
probar "Marcar asistencia"      200,201,400 POST "/asistencias/marcar" "$SOCIO" "1" ""
# Lo que SIEMPRE debe fallar es la segunda marcada del mismo dia.
probar "Marcar dos veces"           400 POST "/asistencias/marcar"     "$SOCIO" "1" ""
# Idem con la salida: la primera cierra el dia, la segunda ya no puede.
probar "Registrar salida"       200,400 PUT  "/asistencias/salida"     "$SOCIO" "1" ""
probar "Salida repetida"            400 PUT  "/asistencias/salida"     "$SOCIO" "1" ""

echo ""
echo "=============================================="
echo " 7. CATALOGOS, ENTRENAMIENTO Y NUTRICION"
echo "=============================================="
probar "Objetivos"                  200 GET  "/catalogos/objetivos"               "$ENTRE" "1" ""
probar "Grupos musculares"          200 GET  "/catalogos/grupos-musculares"       "$ENTRE" "1" ""
probar "Categorias de alimentos"    200 GET  "/catalogos/categorias-de-alimentos" "$ENTRE" "1" ""
probar "Ejercicios"                 200 GET  "/ejercicios"                        "$ENTRE" "1" ""
probar "Ejercicios por grupo"       200 GET  "/ejercicios?idGrupo=1"              "$ENTRE" "1" ""
probar "Alimentos"                  200 GET  "/alimentos"                         "$SOCIO" "1" ""
probar "Plantillas de rutina"       200 GET  "/rutinas"                           "$SOCIO" "1" ""
probar "Socio no crea ejercicio"    403 POST "/ejercicios"                        "$SOCIO" "1" '{"nombre":"Inventado","idGrupo":1}'
probar "Borrar grupo en uso"        400 DELETE "/catalogos/grupos-musculares/1"   "$ENTRE" "1" ""

probar "Generar rutina automatica"  200 POST "/rutinas-de-socio/generar" "$SOCIO" "1" '{"diasPorSemana":3,"ejerciciosPorDia":4}'
probar "Mi rutina"              200,204 GET  "/rutinas-de-socio/mia"     "$SOCIO" "1" ""
probar "Guardar mi rutina"          201 POST "/rutinas-de-socio/mia"     "$SOCIO" "1" '{"nombre":"Rutina de prueba","dias":[{"dia":1,"ejercicios":[{"idEjercicio":1,"series":3,"repeticiones":"10"},{"idEjercicio":2}]}]}'
probar "Rutina invalida (sin dias)" 400 POST "/rutinas-de-socio/mia"     "$SOCIO" "1" '{"nombre":"Vacia","dias":[]}'
probar "Entrenador ve rutina socio" 200 GET  "/rutinas-de-socio/socio/1" "$ENTRE" "1" ""

probar "Generar dieta"              200 POST "/dietas/generar" "$SOCIO" "1" '{"dias":3}'
probar "Guardar mi dieta"           201 POST "/dietas/mia"     "$SOCIO" "1" '{"nombre":"Dieta de prueba","dias":[{"dia":"Dia 1","comidas":[{"categoria":"Desayuno","alimento":"Avena con banana"}]}]}'
probar "Mi dieta"                   200 GET  "/dietas/mia"     "$SOCIO" "1" ""

echo ""
echo "=============================================="
echo " 8. MEDICIONES, HORARIOS Y FORO"
echo "=============================================="
probar "Mis mediciones"             200 GET  "/mediciones/mias"             "$SOCIO" "1" ""
probar "Progreso del socio"         200 GET  "/mediciones/socio/1/progreso" "$ENTRE" "1" ""
probar "Cargar medicion"            201 POST "/mediciones"                  "$ENTRE" "1" '{"idSocio":1,"peso":80.5,"grasaPct":21.0}'
probar "Medicion con peso invalido" 400 POST "/mediciones"                  "$ENTRE" "1" '{"idSocio":1,"peso":0}'
probar "Horarios de la sucursal"    200 GET  "/horarios"                    "$RECEP" "1" ""
probar "Crear horario"              201 POST "/horarios"                    "$RECEP" "1" '{"diaSemana":7,"apertura":"10:00","cierre":"14:00"}'
HOR=$(extraer "d['id']" < "$TMP")
probar "Horario con cierre previo"  400 POST "/horarios"                    "$RECEP" "1" '{"diaSemana":7,"apertura":"18:00","cierre":"09:00"}'
probar "Borrar horario"             204 DELETE "/horarios/$HOR"             "$RECEP" "1" ""
probar "Listar foro"                200 GET  "/foro"                        "$SOCIO" "1" ""

echo ""
echo "=============================================="
echo " 9. AISLAMIENTO ENTRE SUCURSALES"
echo "=============================================="
# El socio 7 es de Sucursal Norte: desde Casa Central no tiene que verse.
probar "Socio de otra sede -> 404"  404 GET  "/socios/7"        "$ADMIN" "1" ""
probar "Socio de otra sede -> OK"   200 GET  "/socios/7"        "$ADMIN" "2" ""
# Pedir explicitamente una sucursal ajena debe dar 403, no caer en la propia en silencio.
probar "Pide sucursal ajena"        403 GET  "/socios/mi-ficha" "$SOCIO" "3" ""
probar "Pide sucursal inexistente"  403 GET  "/socios/mi-ficha" "$SOCIO" "99" ""
probar "Detalle de sucursal ajena"  403 GET  "/sucursales/3"    "$SOCIO" "1" ""

echo ""
echo "=============================================="
echo " 10. ASIGNACION DIRECTA, MULTI-SUCURSAL Y VERIFICACION DE EMAIL"
echo "=============================================="

# --- El entrenador le arma y asigna una rutina directamente a un socio ---
probar "Entrenador asigna rutina"          201 POST "/rutinas-de-socio/asignar" "$ENTRE" "1" '{"idSocio":1,"nombre":"Rutina asignada (prueba)","dias":[{"dia":1,"ejercicios":[{"idEjercicio":1,"series":3,"repeticiones":"10"}]}]}'
# El socio 7 es de Sucursal Norte: no se le puede asignar desde Casa Central.
probar "No asigna a socio de otra sede"    404 POST "/rutinas-de-socio/asignar" "$ENTRE" "1" '{"idSocio":7,"nombre":"Trucho","dias":[{"dia":1,"ejercicios":[{"idEjercicio":1}]}]}'
probar "Socio no asigna rutinas"           403 POST "/rutinas-de-socio/asignar" "$SOCIO" "1" '{"idSocio":1,"nombre":"Trucho","dias":[{"dia":1,"ejercicios":[{"idEjercicio":1}]}]}'

# --- La nutricionista le arma y asigna una dieta directamente a un socio ---
probar "Nutricionista asigna dieta"        201 POST "/dietas/asignar" "$NUTRI" "1" '{"idSocio":1,"nombre":"Dieta asignada (prueba)","dias":[{"dia":"Dia 1","comidas":[{"categoria":"Desayuno","alimento":"Avena"}]}]}'
probar "No asigna dieta a otra sede"       404 POST "/dietas/asignar" "$NUTRI" "1" '{"idSocio":7,"nombre":"Trucho","dias":[{"dia":"Dia 1","comidas":[{"categoria":"Desayuno","alimento":"Avena"}]}]}'

# --- Se registra un socio nuevo y descartable para las pruebas que siguen ---
# Usa un usuario propio (no $SOCIO/Diego) para no ensuciarle el acceso a sucursales:
# la seccion 9 asume que Diego NO es socio de la sucursal 3, y sumarlo ahi rompería
# esa suposicion en la proxima corrida del script.
# Dominio .test (reservado, no existe): con el SMTP configurado el registro manda un mail
# de verdad, y asi no le llega a nadie ni a un dominio ajeno.
CORREO_PRUEBA="verif-prueba-$(date +%s)@vga.test"
CUERPO_REGISTRO="{\"nombre\":\"Prueba\",\"apellido\":\"Verificacion\",\"email\":\"$CORREO_PRUEBA\",\"contrasena\":\"123456\"}"
probar "Registro nuevo (sin verificar)"    201 POST "/auth/register" "" "" "$CUERPO_REGISTRO"

# --- Verificacion de email: el registro NO da acceso hasta confirmar el codigo ---
# La respuesta del registro no puede traer token ni sesion: si los trajera, bastaria con
# ignorar la pantalla del codigo para entrar sin haber verificado nada. Se comprueba que
# traiga la forma nueva (el email al que se mando el codigo) y que no haya token.
EMAIL_DEL_REGISTRO=$(extraer "d['email']" < "$TMP")
TOKEN_DEL_REGISTRO=$(extraer "d.get('token', '')" < "$TMP")
if [ "$EMAIL_DEL_REGISTRO" = "$CORREO_PRUEBA" ] && [ -z "$TOKEN_DEL_REGISTRO" ]; then
  echo "  OK   [201] El registro no devuelve token ni sesion"
  OK=$((OK+1))
else
  echo "  FALLA[201] El registro devolvio token o no devolvio el email pendiente: se entra sin verificar"
  FALLO=$((FALLO+1))
fi

# La cuenta pendiente ya existe: registrarse de nuevo con el mismo email es un conflicto.
probar "Registro repetido: conflicto"      409 POST "/auth/register" "" "" "$CUERPO_REGISTRO"

# Tampoco se entra por el login mientras el email siga sin confirmar.
probar "Login bloqueado sin verificar"     403 POST "/auth/login" "" "" "{\"email\":\"$CORREO_PRUEBA\",\"contrasena\":\"123456\"}"
CODIGO_ERROR=$(extraer "d['codigo']" < "$TMP")
if [ "$CODIGO_ERROR" = "EMAIL_NO_VERIFICADO" ]; then
  echo "  OK   [403] Login bloqueado trae el codigo EMAIL_NO_VERIFICADO"
  OK=$((OK+1))
else
  echo "  FALLA[$CODIGO_ERROR] Login bloqueado no trajo el codigo EMAIL_NO_VERIFICADO esperado"
  FALLO=$((FALLO+1))
fi

# Reenviar y confirmar son anonimos (como la recuperacion de clave): mientras el
# email no este verificado el usuario no puede loguearse, asi que no pueden
# depender de un token propio. Se puede pedir el codigo las veces que haga falta.
probar "Reenviar codigo (1ra vez)"         200 POST "/auth/reenviar-verificacion" "" "" "{\"email\":\"$CORREO_PRUEBA\"}"
probar "Reenviar codigo (2da vez)"         200 POST "/auth/reenviar-verificacion" "" "" "{\"email\":\"$CORREO_PRUEBA\"}"
probar "Reenviar con email inexistente"    200 POST "/auth/reenviar-verificacion" "" "" '{"email":"no-existe-nunca@vga.com"}'
CODIGO=$(codigo_de_verificacion "$CORREO_PRUEBA")
probar "Codigo de verificacion incorrecto" 400 POST "/auth/verificar-email" "" "" "{\"email\":\"$CORREO_PRUEBA\",\"codigo\":\"000000\"}"
probar "Confirmar el email"                200 POST "/auth/verificar-email" "" "" "{\"email\":\"$CORREO_PRUEBA\",\"codigo\":\"$CODIGO\"}"

# Confirmar el codigo es la unica forma de obtener sesion despues de registrarse: devuelve
# el token, asi que no hace falta volver a pedir la contrasena.
NUEVO=$(extraer "d['token']" < "$TMP")
if [ -n "$NUEVO" ]; then
  echo "  OK   [200] Confirmar el email devuelve la sesion"
  OK=$((OK+1))
else
  echo "  FALLA[200] Confirmar el email no devolvio el token de la sesion"
  FALLO=$((FALLO+1))
fi

probar "Login ya funciona verificado"      200 POST "/auth/login" "" "" "{\"email\":\"$CORREO_PRUEBA\",\"contrasena\":\"123456\"}"
probar "Sesion con el email verificado"    200 GET  "/auth/me"                "$NUEVO" "" ""

# --- Ese mismo socio se suma a otra sucursal sin pasar de nuevo por el registro ---
probar "Socio se suma a otra sucursal"     201 POST "/socios/unirme" "$NUEVO" "1" '{"idGimnasio":3}'
probar "No se suma dos veces a la misma"   409 POST "/socios/unirme" "$NUEVO" "1" '{"idGimnasio":3}'
probar "No se suma a sucursal inexistente" 404 POST "/socios/unirme" "$NUEVO" "1" '{"idGimnasio":9999}'

# --- Una sesion vieja de una cuenta sin verificar no se renueva ---
# Se simula una sesion emitida antes de que el registro exigiera el email confirmado: a la
# cuenta que ya tiene token se le saca la verificacion. /auth/me renovaba el token sin
# mirar eso, y asi una sesion de ese tipo se habria mantenido viva para siempre.
marcar_email_verificado "$CORREO_PRUEBA" 0
probar "Sesion vieja sin verificar: 403"   403 GET  "/auth/me"                "$NUEVO" "" ""
CODIGO_ERROR=$(extraer "d['codigo']" < "$TMP")
if [ "$CODIGO_ERROR" = "EMAIL_NO_VERIFICADO" ]; then
  echo "  OK   [403] La sesion vieja trae el codigo EMAIL_NO_VERIFICADO"
  OK=$((OK+1))
else
  echo "  FALLA[$CODIGO_ERROR] La sesion vieja no trajo el codigo EMAIL_NO_VERIFICADO esperado"
  FALLO=$((FALLO+1))
fi
marcar_email_verificado "$CORREO_PRUEBA" 1

echo ""
echo "=============================================="
echo " 11. EDICION: RE-GUARDAR SIN CAMBIOS Y REGLAS DE ROL"
echo "=============================================="
# "Editar" y "Guardar cambios" sin tocar nada tiene que dar 200: el chequeo de nombre
# repetido no puede contar al propio registro como si fuera un duplicado. Se prueba con
# el primer registro de cada listado, reenviando exactamente lo que devolvio la API.

probar "Listar planes"                      200 GET "/planes?incluirInactivos=true" "$ADMIN" "1" ""
ID=$(extraer "d[0]['id']" < "$TMP")
CUERPO=$(extraer "json.dumps({k: d[0][k] for k in ('nombre','descripcion','precio','duracionDias','nivel','accesoMultiSede','activo')})" < "$TMP")
probar "Re-guardar un plan igual"           200 PUT "/planes/$ID"                   "$ADMIN" "1" "$CUERPO"

probar "Listar sucursales"                  200 GET "/sucursales"                   "$ADMIN" "1" ""
ID=$(extraer "d[0]['id']" < "$TMP")
# El cuerpo incluye la ubicacion: el PUT reemplaza todos los campos, asi que si faltara,
# "re-guardar sin cambios" le borraria las coordenadas a la sucursal.
CUERPO=$(extraer "json.dumps({k: d[0][k] for k in ('nombre','direccion','latitud','longitud','idZona','telefono','email')})" < "$TMP")
LATITUD_ANTES=$(extraer "d[0]['latitud']" < "$TMP")
probar "Re-guardar una sucursal igual"      200 PUT "/sucursales/$ID"               "$ADMIN" "1" "$CUERPO"
LATITUD_DESPUES=$(extraer "d['latitud']" < "$TMP")
if [ "$LATITUD_ANTES" = "$LATITUD_DESPUES" ]; then
  echo "  OK   [200] La ubicacion sobrevive a re-guardar la sucursal"
  OK=$((OK+1))
else
  echo "  FALLA[$LATITUD_ANTES -> $LATITUD_DESPUES] Re-guardar la sucursal cambio su latitud"
  FALLO=$((FALLO+1))
fi

# Ubicacion en el mapa: van las dos coordenadas o ninguna, y dentro de rango. Los tres casos
# se rechazan antes de tocar la base, asi que no cambian ningun dato.
# con_ubicacion <cuerpo> <latitud> <longitud>: el mismo cuerpo con la ubicacion pedida (JSON: 95, null...).
con_ubicacion() {
  python -c "import json,sys; d=json.loads(sys.argv[1]); d['latitud']=json.loads(sys.argv[2]); d['longitud']=json.loads(sys.argv[3]); print(json.dumps(d))" "$1" "$2" "$3"
}
probar "Latitud fuera de rango"             400 PUT "/sucursales/$ID"               "$ADMIN" "1" "$(con_ubicacion "$CUERPO" 95 -64.2)"
probar "Latitud sin longitud"               400 PUT "/sucursales/$ID"               "$ADMIN" "1" "$(con_ubicacion "$CUERPO" -31.4 null)"
probar "Longitud sin latitud"               400 PUT "/sucursales/$ID"               "$ADMIN" "1" "$(con_ubicacion "$CUERPO" null -64.2)"

probar "Listar horarios"                    200 GET "/horarios"                     "$RECEP" "1" ""
ID=$(extraer "d[0]['id']" < "$TMP")
CUERPO=$(extraer "json.dumps({k: d[0][k] for k in ('diaSemana','apertura','cierre')})" < "$TMP")
probar "Re-guardar un horario igual"        200 PUT "/horarios/$ID"                 "$RECEP" "1" "$CUERPO"

probar "Listar formas de pago"              200 GET "/formas-de-pago?incluirInactivas=true" "$ADMIN" "1" ""
ID=$(extraer "d[0]['id']" < "$TMP")
CUERPO=$(extraer "json.dumps({k: d[0][k] for k in ('nombre','activo')})" < "$TMP")
probar "Re-guardar una forma de pago igual" 200 PUT "/formas-de-pago/$ID"           "$ADMIN" "1" "$CUERPO"

probar "Listar objetivos"                   200 GET "/catalogos/objetivos"          "$ENTRE" "1" ""
ID=$(extraer "d[0]['id']" < "$TMP")
CUERPO=$(extraer "json.dumps({'nombre': d[0]['nombre']})" < "$TMP")
probar "Re-guardar un objetivo igual"       200 PUT "/catalogos/objetivos/$ID"      "$ENTRE" "1" "$CUERPO"

probar "Listar ejercicios"                  200 GET "/ejercicios"                   "$ENTRE" "1" ""
ID=$(extraer "d[0]['id']" < "$TMP")
CUERPO=$(extraer "json.dumps({k: d[0][k] for k in ('nombre','descripcion','idGrupo','videoUrl')})" < "$TMP")
probar "Re-guardar un ejercicio igual"      200 PUT "/ejercicios/$ID"               "$ENTRE" "1" "$CUERPO"

probar "Listar alimentos"                   200 GET "/alimentos"                    "$NUTRI" "1" ""
ID=$(extraer "d[0]['id']" < "$TMP")
CUERPO=$(extraer "json.dumps({k: d[0][k] for k in ('nombre','calorias','proteinas','carbohidratos','grasas','descripcion','idCategoria','idObjetivo')})" < "$TMP")
probar "Re-guardar un alimento igual"       200 PUT "/alimentos/$ID"                "$NUTRI" "1" "$CUERPO"

probar "Pie de pagina"                      200 GET "/sitio-web/footer/publico"     "" "" ""
ID=$(extraer "list(d.values())[0][0]['id']" < "$TMP")
CUERPO=$(extraer "json.dumps({'seccion': list(d.keys())[0], 'clave': list(d.values())[0][0]['clave'], 'valor': list(d.values())[0][0]['valor'], 'orden': list(d.values())[0][0]['orden']})" < "$TMP")
probar "Re-guardar un dato del pie igual"   200 PUT "/sitio-web/footer/$ID"         "$ADMIN" "1" "$CUERPO"

# El formulario de edicion de personal manda siempre el rol. Volver a guardar el mismo
# no es "designar" a nadie: un administrador tiene que poder editarse a si mismo, pero
# no ascender a otra persona a administrador.
probar "Roles asignables"                   200 GET "/usuarios/roles"               "$ADMIN" "1" ""
ROL_ADMIN=$(extraer "[r['id'] for r in d if r['nombre']=='Administrador'][0]" < "$TMP")
probar "Listar el personal"                 200 GET "/usuarios?tamanioPagina=50"    "$ADMIN" "1" ""
ID_ADMIN=$(extraer "[u['id'] for u in d['items'] if u['username']=='admin'][0]" < "$TMP")
CUERPO_ADMIN=$(extraer "json.dumps({k: [u for u in d['items'] if u['username']=='admin'][0][k] for k in ('nombre','apellido','telefono','username','activo','idRol')})" < "$TMP")
ID_ENTRE=$(extraer "[u['id'] for u in d['items'] if u['username']=='entrenador'][0]" < "$TMP")
CUERPO_ENTRE=$(extraer "json.dumps({k: [u for u in d['items'] if u['username']=='entrenador'][0][k] for k in ('nombre','apellido','telefono','username','activo','idRol')})" < "$TMP")
CUERPO_ASCENSO=$(python -c "import json,sys;b=json.loads(sys.argv[1]);b['idRol']=int(sys.argv[2]);print(json.dumps(b))" "$CUERPO_ENTRE" "$ROL_ADMIN")
probar "Admin se edita a si mismo"          200 PUT "/usuarios/$ID_ADMIN"           "$ADMIN" "1" "$CUERPO_ADMIN"
probar "Admin re-guarda el rol de otro"     200 PUT "/usuarios/$ID_ENTRE"           "$ADMIN" "1" "$CUERPO_ENTRE"
probar "Admin no asciende a otro a admin"   403 PUT "/usuarios/$ID_ENTRE"           "$ADMIN" "1" "$CUERPO_ASCENSO"
probar "Duenio si asciende a admin"         200 PUT "/usuarios/$ID_ENTRE"           "$DUENIO" "1" "$CUERPO_ASCENSO"
probar "Duenio lo devuelve a su rol"        200 PUT "/usuarios/$ID_ENTRE"           "$DUENIO" "1" "$CUERPO_ENTRE"

echo ""
echo "=============================================="
echo " 12. ALTA DE PERSONAL: LA CUENTA NACE SIN VERIFICAR"
echo "=============================================="
# La cuenta que da de alta la administracion nace con el email sin verificar, igual que
# en el registro propio: nadie comprobo que ese mail sea de esa persona. Se le manda el
# codigo en el acto y, la primera vez que ingrese, tiene que confirmarlo.
# Se usa un dominio .test (reservado, no existe) para no mandarle mails de prueba a un
# dominio real: si el SMTP esta configurado, el envio sale igual hacia ese destino.
CORREO_ALTA="alta-prueba-$(date +%s)@vga.test"
probar "Roles para el alta"                200 GET  "/usuarios/roles" "$ADMIN" "1" ""
ROL_RECEP=$(extraer "[r['id'] for r in d if r['nombre']=='Recepcionista'][0]" < "$TMP")
probar "Admin da de alta a una persona"    201 POST "/usuarios"       "$ADMIN" "1" "{\"nombre\":\"Alta\",\"apellido\":\"Prueba\",\"email\":\"$CORREO_ALTA\",\"contrasena\":\"123456\",\"idRol\":$ROL_RECEP}"
probar "Mismo email: no se repite"         409 POST "/usuarios"       "$ADMIN" "1" "{\"nombre\":\"Alta\",\"apellido\":\"Repetida\",\"email\":\"$CORREO_ALTA\",\"contrasena\":\"123456\",\"idRol\":$ROL_RECEP}"

probar "Dada de alta: login bloqueado"     403 POST "/auth/login" "" "" "{\"email\":\"$CORREO_ALTA\",\"contrasena\":\"123456\"}"
CODIGO_ERROR=$(extraer "d['codigo']" < "$TMP")
if [ "$CODIGO_ERROR" = "EMAIL_NO_VERIFICADO" ]; then
  echo "  OK   [403] Dada de alta: pide verificar el email (EMAIL_NO_VERIFICADO)"
  OK=$((OK+1))
else
  echo "  FALLA[$CODIGO_ERROR] Dada de alta: no pidio verificar el email"
  FALLO=$((FALLO+1))
fi

# Sin haber pedido ningun reenvio, tiene que haber un codigo vigente: el del alta.
CODIGO_ALTA=$(codigo_de_verificacion "$CORREO_ALTA")
if [ -n "$CODIGO_ALTA" ]; then
  echo "  OK   [BD] El alta le emitio el codigo de verificacion en el acto"
  OK=$((OK+1))
else
  echo "  FALLA[BD] El alta no le emitio ningun codigo de verificacion"
  FALLO=$((FALLO+1))
fi

probar "Confirma el email con ese codigo"  200 POST "/auth/verificar-email" "" "" "{\"email\":\"$CORREO_ALTA\",\"codigo\":\"$CODIGO_ALTA\"}"
probar "Ya puede ingresar"                 200 POST "/auth/login" "" "" "{\"email\":\"$CORREO_ALTA\",\"contrasena\":\"123456\"}"

limpiar_usuario_de_prueba "$CORREO_PRUEBA"
limpiar_usuario_de_prueba "$CORREO_ALTA"
rm -f "$TMP"

echo ""
echo "=============================================="
echo " RESULTADO:  $OK correctas / $FALLO fallidas"
echo "=============================================="
[ "$FALLO" -eq 0 ] || exit 1
