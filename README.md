# VirtualGymAssistant - BackEnd

API REST de gestión integral para gimnasios multi-sucursal, desarrollada en
**ASP.NET Core 10** siguiendo **Clean Architecture**.

Trabajo Final Integrador - Tecnicatura Universitaria en Programación (UTN).

---

## Cómo levantarlo

**Requisitos:** .NET 10 SDK y SQL Server con la base `VirtualGymAssistant` ya creada
(scripts `01_EsquemaBD.sql` y `02_Contenido.sql`).

```bash
dotnet run --project src/VirtualGymAssistant.API
```

La API queda escuchando en `https://localhost:7180` (o `http://localhost:5180`) y
**Swagger se abre en la raíz**: entrando a esa dirección ya se ve la documentación
interactiva con todos los endpoints.

> **Si tu base ya estaba creada** (de antes de que las sucursales tuvieran ubicación en el
> mapa), volvé a correr `01_EsquemaBD.sql` y `02_Contenido.sql`: los dos son idempotentes.
> Agregan las columnas `Latitud` y `Longitud` a `dbo.Gimnasios` y cargan las coordenadas de
> las tres sedes demo. Sin eso la API falla con `Invalid column name 'Latitud'`.

### Cadena de conexión

Está en `src/VirtualGymAssistant.API/appsettings.json`:

```json
"ConnectionStrings": {
  "VirtualGymAssistant": "Data Source=PC_JOTA\\SQL2017;Database=VirtualGymAssistant;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Cada integrante cambia el `Data Source` por el nombre de su instancia de SQL Server.

> **Importante:** antes de entregar o publicar, reemplazar `Jwt:ClaveSecreta` en
> `appsettings.json` por una cadena larga y secreta (mínimo 32 caracteres).

---

## Arquitectura

El proyecto está dividido en cuatro capas. La regla de dependencia es **siempre hacia
adentro**: una capa sólo puede conocer a las que están debajo de ella.

```
VirtualGymAssistant.API              (controladores, middleware, Swagger, JWT)
            |
            v
VirtualGymAssistant.Infrastructure   (EF Core, DbContext, BCrypt, archivos)
            |
            v
VirtualGymAssistant.Application      (casos de uso, DTOs, validaciones)
            |
            v
VirtualGymAssistant.Domain           (entidades, reglas, excepciones)
```

| Capa | Qué contiene | De qué NO depende |
|---|---|---|
| **Domain** | Entidades, constantes de negocio (roles, estados) y excepciones. | De nada. Es C# puro, sin paquetes externos. |
| **Application** | Un servicio por caso de uso, sus DTOs y sus validadores. Define *interfaces* (puertos) para todo lo que necesita de afuera. | De EF Core concreto, de SQL Server y de HTTP. |
| **Infrastructure** | Implementa esas interfaces: `DbContext`, mapeos, JWT, BCrypt, guardado de archivos. | De ASP.NET Core. |
| **API** | Controladores finos, middleware de errores, filtro de validación y configuración. | — |

### Por qué importa esta separación

La lógica de negocio no sabe que existe HTTP ni SQL Server. Por ejemplo, el servicio de
pagos lanza `ReglaDeNegocioException("El mes ya tiene un pago aprobado")` y **nunca**
decide un código HTTP: eso lo traduce el middleware. Si mañana se cambia SQL Server por
otro motor, o se agrega una app de escritorio además de la web, la capa Application no
se toca.

---

## Estructura de carpetas

```
src/
├── VirtualGymAssistant.Domain/
│   ├── Entities/          34 entidades mapeadas a las tablas de la base
│   ├── Constants/         RolesDelSistema, EstadosDeSocio, EstadosDePago
│   └── Exceptions/        Excepciones de negocio (una por tipo de error)
│
├── VirtualGymAssistant.Application/
│   ├── Common/
│   │   ├── Interfaces/    Puertos: IVirtualGymDbContext, IUsuarioActual, IServicioDeTokens...
│   │   ├── Models/        ContextoDeUsuario, ResultadoPaginado, ParametrosDePaginacion
│   │   ├── Extensions/    Helpers de consulta y de texto
│   │   └── Constants/     Meses, DiasDeLaSemana
│   └── Features/          Una carpeta por módulo de negocio:
│       ├── Autenticacion/  Contexto/     Organizaciones/  Sucursales/
│       ├── Usuarios/       Socios/       Planes/          Membresias/
│       ├── FormasDePago/   Pagos/        Asistencias/     Horarios/
│       ├── Mediciones/     Catalogos/    Ejercicios/      Alimentos/
│       └── Rutinas/        Dietas/       SitioWeb/        Foro/
│
├── VirtualGymAssistant.Infrastructure/
│   ├── Persistence/
│   │   ├── VirtualGymDbContext.cs
│   │   └── Configurations/   Mapeo Fluent API a las tablas existentes
│   ├── Seguridad/            JWT y hash de contraseñas
│   └── Servicios/            Reloj, archivos, notificaciones
│
└── VirtualGymAssistant.API/
    ├── Controllers/       21 controladores
    ├── Middleware/        Manejo global de excepciones
    ├── Filtros/           Validación automática
    ├── Configuracion/     Swagger
    └── Servicios/         UsuarioActual (lee el token y la cabecera de sucursal)
```

Cada módulo de `Features/` sigue el mismo patrón: `Dtos/` (lo que entra y sale),
`Validadores/` (reglas de formato) y un `ServicioDeXxx.cs` con los casos de uso.

---

## El modelo multi-sucursal

Es la decisión de diseño central del sistema y conviene entenderla antes de tocar código.

- Un **dueño** es titular de una **organización**, que agrupa una o varias **sucursales**.
- Hay **una sola tabla de usuarios**: dueños, personal y socios comparten identidad.
- Los **permisos se asignan por sucursal** (`UsuarioGimnasioRol`): la misma persona puede
  ser Entrenador en una sede y Recepcionista en otra.

Por eso **no alcanza con saber quién es el usuario**: hay que saber además sobre qué
sucursal está trabajando. Eso lo resuelve `ServicioDeContexto`, que en cada petición arma
un `ContextoDeUsuario` con:

- todas las sucursales donde el usuario puede operar y su rol más alto en cada una,
- la sucursal activa (la que llega en la cabecera `X-Gimnasio-Id`),
- si es dueño de alguna organización.

### La cabecera `X-Gimnasio-Id`

El front la manda en cada petición para indicar la sucursal activa:

```http
GET /api/socios
Authorization: Bearer <token>
X-Gimnasio-Id: 2
```

- Si **no se envía**, se usa la primera sucursal del usuario (la casa matriz si la tiene).
- Si se envía **una sucursal a la que el usuario no tiene acceso**, la API responde **403**.
  No cae en otra sede en silencio, a propósito: si no, un recepcionista podría creer que
  está cobrando en una sucursal y estar cobrando en otra.

### Los roles no van en el token

El JWT lleva sólo la identidad (id, email, username). Los roles se resuelven contra la
base en cada petición. Es una consulta más, pero significa que un cambio de permisos tiene
efecto **inmediato**: si los roles viajaran firmados dentro del token, recién se
actualizarían cuando el token venciera.

---

## Manejo de errores

Los servicios lanzan la excepción que corresponde al problema y
`ManejadorGlobalDeExcepciones` la traduce a HTTP en un solo lugar:

| Excepción | HTTP | Cuándo se usa |
|---|---|---|
| `ValidacionException` | 400 | Datos con formato inválido (incluye el detalle campo por campo) |
| `ReglaDeNegocioException` | 400 | Rompe una regla del negocio (ej: marcar asistencia dos veces el mismo día) |
| `NoAutenticadoException` | 401 | Falta el token o las credenciales son incorrectas |
| `AccesoDenegadoException` | 403 | Autenticado pero sin permiso sobre el recurso |
| `NoEncontradoException` | 404 | El recurso no existe o no pertenece al contexto actual |
| `ConflictoException` | 409 | Choca con un registro existente (email repetido, nombre duplicado) |

Todas las respuestas de error usan el formato **ProblemDetails** (RFC 7807), que es el
estándar de ASP.NET Core:

```json
{
  "title": "No se puede completar la operación.",
  "status": 400,
  "detail": "Diciembre de 2026 ya tiene un pago en estado Pendiente. No se puede volver a pagar ese período.",
  "instance": "/api/pagos",
  "traceId": "0HNOKCA3VIA9I:00000003"
}
```

---

## Reglas de negocio implementadas

Las que están garantizadas también por índices únicos en la base:

- **Un solo pago por socio y período** (año + mes). Un pago rechazado o vencido se
  *reintenta* reutilizando el mismo registro, no se crea otro.
- **Una sola asistencia por socio y por día.**
- **Una sola membresía activa por socio.** Cambiar de plan cierra la anterior y abre una
  nueva, para que el historial quede completo.
- **Un número de socio correlativo por sucursal.**

Reglas que aplica la capa Application:

- **Sin email confirmado no hay sesión.** Registrarse crea la cuenta pero **no devuelve
  token** (responde 201 con el email al que se mandó el código). La sesión se emite recién
  al confirmar el código de 6 dígitos (`POST /api/auth/verificar-email`); hasta entonces el
  login responde 403 con el código `EMAIL_NO_VERIFICADO`. Lo mismo vale para las cuentas
  que da de alta la administración, y `/api/auth/me` tampoco renueva el token de una cuenta
  sin confirmar.
- **Ubicación de la sucursal en el mapa.** `Latitud` y `Longitud` son opcionales, van las dos
  o ninguna y dentro de rango (lo valida el backend y lo garantiza un CHECK en la base).
  `GET /api/sucursales/publicas` las devuelve para que el sitio dibuje el mapa; una sucursal
  sin coordenadas aparece en la lista pero sin pin. Ojo: `PUT /api/sucursales/{id}` reemplaza
  todos los campos, así que quien edite una sucursal tiene que reenviar también la ubicación.
- **Creación automática de la ficha de socio** (`GestorDeFichasDeSocio`): al registrarse,
  pagar, marcar asistencia o guardar una rutina en una sucursal donde todavía no es socio,
  el sistema le crea la ficha.
- **Transiciones válidas del estado de un pago**: por ejemplo, un pago Anulado es estado
  final y no se puede volver a aprobar.
- **Al aprobarse un pago**, el socio queda con una membresía activa del plan pagado.
- **Sólo el dueño designa administradores**, y el rol Dueño no se asigna a mano: surge de
  ser titular de la organización.
- **Bajas lógicas** en sucursales, planes, usuarios y formas de pago, para no romper el
  historial. Las formas de pago y los planes ya usados nunca se borran físicamente.
- **La organización no puede quedarse sin casa matriz**: si se da de baja la que lo era,
  el rol pasa automáticamente a otra sucursal activa.

---

## Usuarios de prueba

Están cargados por el script `02_Contenido.sql`:

| Email | Rol | Sucursales |
|---|---|---|
| `duenio@vga.com` | Dueño | Las 3 |
| `admin@vga.com` | Administrador | Las 3 |
| `recepcion@vga.com` | Recepcionista | Casa Central |
| `entrenador@vga.com` | Entrenador | Casa Central y Norte |
| `nutricionista@vga.com` | Nutricionista | Casa Central |
| `socio@vga.com` | Socio | Casa Central |

La contraseña del seed original es `123456`. Ojo: si alguien ya cambió una contraseña
desde la aplicación, la del seed deja de valer (por ejemplo, en la base actual
`admin@vga.com` tiene la contraseña `admin`).

---

## Decisiones técnicas y por qué

**Entity Framework Core mapeado a un esquema existente, sin migraciones.**
El esquema lo definen los scripts SQL del proyecto, que son la fuente de verdad. Por eso
cada entidad declara explícitamente su tabla y sus columnas en `Persistence/Configurations/`.
Si se agrega una columna, se modifica el script SQL **y** su configuración.

**BCrypt para las contraseñas, no el hasher de ASP.NET Identity.**
La base ya tenía hashes en formato BCrypt generados por la versión anterior del backend,
así que los usuarios existentes siguen pudiendo iniciar sesión sin migrar nada.

**Servicios de aplicación en lugar de MediatR/CQRS.**
Para el tamaño de este sistema, un servicio por módulo es más directo de leer y de
depurar que una cadena de handlers, y no agrega dependencias externas.

**`IVirtualGymDbContext` como puerto de persistencia.**
La capa Application consulta a través de esta interfaz y no del `DbContext` concreto, así
el negocio no queda atado al motor de base de datos y se puede sustituir por una
implementación en memoria para los tests.

**Hora local, no UTC.**
Los períodos de cuota, las asistencias diarias y los horarios de apertura son conceptos
del huso horario del gimnasio. Guardar UTC haría que un check-in de las 22 hs figure al
día siguiente.

---

## Probar la API

Hay tres formas:

1. **Swagger** (recomendado): entrar a la raíz, hacer login con `POST /api/auth/login`,
   copiar el `token`, tocar **Authorize** y pegarlo. Después se puede completar la
   cabecera `X-Gimnasio-Id` en cualquier endpoint.
2. **El archivo `VirtualGymAssistant.API.http`**, desde Visual Studio o VS Code. Guarda el
   token automáticamente después del login.
3. **Postman**, importando el `swagger.json` desde `/swagger/v1/swagger.json`.
