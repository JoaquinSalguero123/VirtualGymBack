using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Extensions;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Features.Autenticacion.Dtos;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Entities;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.Autenticacion;

/// <summary>
/// Casos de uso de cuenta y acceso: registro, login, refresco de sesión y
/// recuperación / cambio de contraseña.
/// </summary>
public class ServicioDeAutenticacion
{
    /// <summary>
    /// Mensaje para la cuenta que todavía no confirmó su email. Lo comparten el login y el
    /// refresco de sesión, así el front muestra lo mismo en los dos casos.
    /// </summary>
    private const string MensajeEmailNoVerificado =
        "Todavía no confirmaste tu email. Reenviá el código y confirmalo para poder ingresar.";

    private readonly IVirtualGymDbContext _db;
    private readonly IHasheadorDeClaves _hasheador;
    private readonly IServicioDeTokens _tokens;
    private readonly IServicioDeContexto _contexto;
    private readonly IProveedorDeFechaHora _reloj;
    private readonly IServicioDeNotificaciones _notificaciones;
    private readonly GestorDeVerificacionDeEmail _verificacion;

    public ServicioDeAutenticacion(
        IVirtualGymDbContext db,
        IHasheadorDeClaves hasheador,
        IServicioDeTokens tokens,
        IServicioDeContexto contexto,
        IProveedorDeFechaHora reloj,
        IServicioDeNotificaciones notificaciones,
        GestorDeVerificacionDeEmail verificacion)
    {
        _db = db;
        _hasheador = hasheador;
        _tokens = tokens;
        _contexto = contexto;
        _reloj = reloj;
        _notificaciones = notificaciones;
        _verificacion = verificacion;
    }

    /// <summary>
    /// Caso de uso "Registrarse en una sucursal".
    /// Crea el usuario, le arma la ficha de socio en la sucursal elegida y le asigna el rol Socio.
    ///
    /// NO inicia sesión: la cuenta queda creada pero sin acceso hasta que confirme el código
    /// que se le manda por mail. La sesión recién se emite en <see cref="VerificarEmailAsync"/>.
    /// </summary>
    public async Task<RegistroPendienteDto> RegistrarAsync(RegistroRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Usuarios.AnyAsync(u => u.Mail == email, ct))
            throw new ConflictoException("Ya existe una cuenta registrada con ese email.");

        var telefono = request.Telefono.NormalizarONull();
        if (telefono is not null && await _db.Usuarios.AnyAsync(u => u.Telefono == telefono, ct))
            throw new ConflictoException("Ya existe una cuenta registrada con ese teléfono.");

        var gimnasio = await ResolverGimnasioDeAltaAsync(request.IdGimnasio, ct);

        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(),
            Apellido = request.Apellido.Trim(),
            Mail = email,
            Telefono = telefono,
            Clave = _hasheador.Hashear(request.Contrasena),
            Username = await GenerarUsernameUnicoAsync(email.Split('@')[0], ct),
            Activo = true,
            FechaAlta = _reloj.Ahora
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync(ct);

        await CrearFichaDeSocioAsync(usuario.Id, gimnasio.Id, ct);
        await AsignarRolAsync(usuario.Id, gimnasio.Id, RolesDelSistema.Socio, ct);
        await _db.SaveChangesAsync(ct);

        // Si el mail no sale, la cuenta igual queda guardada (ver GestorDeVerificacionDeEmail):
        // la pantalla del código ofrece reenviarlo.
        await _verificacion.EnviarCodigoAsync(usuario, ct);

        return new RegistroPendienteDto
        {
            Email = usuario.Mail,
            Mensaje = $"Te mandamos un código de 6 dígitos a {usuario.Mail}. Ingresalo para activar " +
                      $"tu cuenta; vence en {GestorDeVerificacionDeEmail.MinutosDeVigenciaDelCodigo} minutos."
        };
    }

    /// <summary>Caso de uso "Iniciar sesión".</summary>
    public async Task<SesionDto> IniciarSesionAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Mail == email, ct);

        // Mismo mensaje para "no existe" y "clave incorrecta": si fueran distintos,
        // cualquiera podría averiguar qué emails están registrados probando el login.
        if (usuario is null || !_hasheador.Verificar(request.Contrasena, usuario.Clave))
            throw new NoAutenticadoException("Email o contraseña incorrectos.");

        if (!usuario.Activo)
            throw new AccesoDenegadoException("La cuenta está desactivada. Contactate con el gimnasio.");

        // No se reenvía el código automáticamente acá: si se hiciera, cada intento de
        // login fallido (por ejemplo con la clave mal tipeada) mandaría un mail nuevo.
        // El front ofrece un botón de "reenviar" a mano en esta misma pantalla.
        if (!usuario.EmailVerificado)
            throw new EmailNoVerificadoException(MensajeEmailNoVerificado);

        return await ArmarSesionAsync(usuario, ct);
    }

    /// <summary>
    /// Devuelve la sesión del usuario autenticado con los datos frescos de la base.
    /// El front la usa al recargar la página y después de cambiar de sucursal.
    /// </summary>
    public async Task<SesionDto> ObtenerSesionActualAsync(int idUsuario, CancellationToken ct = default)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == idUsuario, ct)
            ?? throw new NoEncontradoException("el usuario", idUsuario);

        // Hoy ninguna sesión se emite sin el email confirmado (ver RegistrarAsync e
        // IniciarSesionAsync), pero un token emitido cuando el registro todavía dejaba entrar
        // se seguiría renovando desde acá hasta el infinito. Con esto se corta en el primer
        // refresco, que el front hace al recargar la página.
        if (!usuario.EmailVerificado)
            throw new EmailNoVerificadoException(MensajeEmailNoVerificado);

        return await ArmarSesionAsync(usuario, ct);
    }

    /// <summary>
    /// Caso de uso "Recuperar contraseña" (paso 1): genera un código de un solo uso.
    /// Responde siempre lo mismo, exista o no el email, para no filtrar qué cuentas hay.
    /// </summary>
    public async Task<SolicitudRecuperacionDto> SolicitarRecuperacionAsync(
        SolicitarRecuperacionRequest request, CancellationToken ct = default)
    {
        const string mensajeGenerico =
            "Si el email está registrado, vas a recibir un código para restablecer tu contraseña.";

        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Mail == email && u.Activo, ct);

        if (usuario is null)
            return new SolicitudRecuperacionDto { Mensaje = mensajeGenerico };

        // Se invalidan los códigos anteriores para que sólo el último quede vigente.
        var anteriores = await _db.TokensRecuperacion
            .Where(t => t.IdUsuario == usuario.Id && !t.Usado)
            .ToListAsync(ct);

        foreach (var token in anteriores) token.Usado = true;

        var codigo = GestorDeVerificacionDeEmail.GenerarCodigoDeUnSoloUso();

        _db.TokensRecuperacion.Add(new TokenRecuperacion
        {
            IdUsuario = usuario.Id,
            Codigo = codigo,
            Token = Guid.NewGuid().ToString("N"),
            Canal = "Mail",
            Expira = _reloj.Ahora.AddMinutes(GestorDeVerificacionDeEmail.MinutosDeVigenciaDelCodigo),
            Usado = false,
            FechaCreacion = _reloj.Ahora
        });

        await _db.SaveChangesAsync(ct);
        await _notificaciones.EnviarCodigoDeRecuperacionAsync(usuario.Mail, codigo, ct);

        return new SolicitudRecuperacionDto { Mensaje = mensajeGenerico };
    }

    /// <summary>Caso de uso "Recuperar contraseña" (paso 2): valida el código y guarda la nueva clave.</summary>
    public async Task RestablecerClaveAsync(RestablecerClaveRequest request, CancellationToken ct = default)
    {
        if (request.NuevaClave != request.ConfirmacionClave)
            throw new ValidacionException(nameof(request.ConfirmacionClave), "Las contraseñas no coinciden.");

        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Mail == email, ct)
            ?? throw new ReglaDeNegocioException("El código no es válido o ya venció.");

        var token = await _db.TokensRecuperacion
            .Where(t => t.IdUsuario == usuario.Id
                        && t.Codigo == request.Codigo
                        && !t.Usado
                        && t.Expira > _reloj.Ahora)
            .OrderByDescending(t => t.FechaCreacion)
            .FirstOrDefaultAsync(ct)
            ?? throw new ReglaDeNegocioException("El código no es válido o ya venció.");

        usuario.Clave = _hasheador.Hashear(request.NuevaClave);
        token.Usado = true;

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Cambio de contraseña desde el perfil, con verificación de la clave actual.</summary>
    public async Task CambiarClaveAsync(int idUsuario, CambiarClaveRequest request, CancellationToken ct = default)
    {
        if (request.NuevaClave != request.ConfirmacionClave)
            throw new ValidacionException(nameof(request.ConfirmacionClave), "Las contraseñas no coinciden.");

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == idUsuario, ct)
            ?? throw new NoEncontradoException("el usuario", idUsuario);

        if (!_hasheador.Verificar(request.ClaveActual, usuario.Clave))
            throw new ReglaDeNegocioException("La contraseña actual es incorrecta.");

        usuario.Clave = _hasheador.Hashear(request.NuevaClave);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Caso de uso "Verificar el email": valida el código y confirma la cuenta.
    /// Es anónimo, igual que <see cref="RestablecerClaveAsync"/>: mientras el email no
    /// esté verificado el usuario no puede loguearse (ver <see cref="IniciarSesionAsync"/>),
    /// así que esto no puede depender de un token propio. Si el código es válido devuelve
    /// la sesión ya iniciada, para no pedirle la contraseña de nuevo justo después de
    /// confirmar. Tras registrarse es la única forma de obtener sesión: el registro no la emite.
    /// </summary>
    public async Task<SesionDto> VerificarEmailAsync(VerificarEmailRequest request, CancellationToken ct = default)
    {
        const string mensajeError = "El código no es válido o ya venció.";

        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Mail == email && u.Activo, ct)
            ?? throw new ReglaDeNegocioException(mensajeError);

        // Ya verificado: se rechaza con el mismo mensaje genérico en vez de un "ya estás
        // verificado, andá a loguearte" — si no, cualquiera podría usar este endpoint
        // anónimo para averiguar qué emails están registrados y ya confirmados.
        if (usuario.EmailVerificado)
            throw new ReglaDeNegocioException(mensajeError);

        var token = await _db.TokensVerificacionEmail
            .Where(t => t.IdUsuario == usuario.Id
                        && t.Codigo == request.Codigo
                        && !t.Usado
                        && t.Expira > _reloj.Ahora)
            .OrderByDescending(t => t.FechaCreacion)
            .FirstOrDefaultAsync(ct)
            ?? throw new ReglaDeNegocioException(mensajeError);

        usuario.EmailVerificado = true;
        token.Usado = true;

        await _db.SaveChangesAsync(ct);

        return await ArmarSesionAsync(usuario, ct);
    }

    /// <summary>
    /// Genera y reenvía el código de verificación. Anónimo, con la misma respuesta
    /// siempre exista o no el email o ya esté verificado (mismo criterio de privacidad
    /// que <see cref="SolicitarRecuperacionAsync"/>), y sin límite de veces: el usuario
    /// puede pedir un código nuevo todas las veces que haga falta.
    /// </summary>
    public async Task<ReenvioDeVerificacionDto> ReenviarVerificacionAsync(
        ReenviarVerificacionRequest request, CancellationToken ct = default)
    {
        const string mensajeGenerico =
            "Si el email está registrado y todavía no fue verificado, te mandamos un código nuevo.";

        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Mail == email && u.Activo, ct);

        if (usuario is null || usuario.EmailVerificado)
            return new ReenvioDeVerificacionDto { Mensaje = mensajeGenerico };

        await _verificacion.EnviarCodigoAsync(usuario, ct);

        return new ReenvioDeVerificacionDto { Mensaje = mensajeGenerico };
    }

    // ------------------------------------------------------------------
    // Ayudantes internos
    // ------------------------------------------------------------------

    /// <summary>Genera el token y empaqueta el perfil con sus sucursales.</summary>
    private async Task<SesionDto> ArmarSesionAsync(Usuario usuario, CancellationToken ct)
    {
        var sucursales = await _contexto.ObtenerSucursalesAsync(usuario.Id, ct);
        var esDuenio = await _db.Organizaciones.AnyAsync(o => o.IdDuenio == usuario.Id, ct);
        var (token, expira) = _tokens.GenerarToken(usuario);

        return new SesionDto
        {
            Token = token,
            ExpiraEn = expira,
            Usuario = new UsuarioSesionDto
            {
                Id = usuario.Id,
                Username = usuario.Username,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Email = usuario.Mail,
                Telefono = usuario.Telefono,
                Activo = usuario.Activo,
                EmailVerificado = usuario.EmailVerificado,
                EsDuenio = esDuenio,
                Sucursales = sucursales,
                RolPrincipal = sucursales.FirstOrDefault()?.Rol
            }
        };
    }

    /// <summary>
    /// Elige la sucursal de alta: la pedida, o la casa matriz, o la primera activa.
    /// </summary>
    private async Task<Gimnasio> ResolverGimnasioDeAltaAsync(int? idPedido, CancellationToken ct)
    {
        if (idPedido is { } id)
        {
            return await _db.Gimnasios.FirstOrDefaultAsync(g => g.Id == id && g.Activo, ct)
                ?? throw new NoEncontradoException("La sucursal elegida no existe o no está activa.");
        }

        return await _db.Gimnasios
            .Where(g => g.Activo)
            .OrderByDescending(g => g.EsCasaMatriz)
            .ThenBy(g => g.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new ReglaDeNegocioException(
                "Todavía no hay sucursales activas en el sistema, así que no se puede registrar socios.");
    }

    /// <summary>
    /// Busca un username libre. Si "juan" está tomado prueba "juan1", "juan2", etc.
    /// </summary>
    private async Task<string> GenerarUsernameUnicoAsync(string baseDeseada, CancellationToken ct)
    {
        var raiz = baseDeseada.ASlugDeUsuario();
        var candidato = raiz;
        var intento = 1;

        while (await _db.Usuarios.AnyAsync(u => u.Username == candidato, ct))
            candidato = $"{raiz}{intento++}";

        return candidato;
    }

    /// <summary>Crea la ficha de socio si el usuario todavía no es socio de esa sucursal.</summary>
    private async Task CrearFichaDeSocioAsync(int idUsuario, int idGimnasio, CancellationToken ct)
    {
        var yaEsSocio = await _db.Socios
            .AnyAsync(s => s.IdUsuario == idUsuario && s.IdGimnasio == idGimnasio, ct);

        if (yaEsSocio) return;

        var idEstadoActivo = await _db.SocioEstados
            .Where(e => e.Nombre == EstadosDeSocio.Activo)
            .Select(e => e.Id)
            .FirstAsync(ct);

        // El número de socio es correlativo dentro de cada sucursal.
        var ultimoNumero = await _db.Socios
            .Where(s => s.IdGimnasio == idGimnasio && s.NumeroSocio != null)
            .MaxAsync(s => (int?)s.NumeroSocio, ct) ?? 0;

        _db.Socios.Add(new Socio
        {
            IdUsuario = idUsuario,
            IdGimnasio = idGimnasio,
            IdEstado = idEstadoActivo,
            NumeroSocio = ultimoNumero + 1,
            FechaAlta = _reloj.Hoy
        });
    }

    /// <summary>Asigna un rol en una sucursal si todavía no lo tiene.</summary>
    private async Task AsignarRolAsync(int idUsuario, int idGimnasio, string nombreRol, CancellationToken ct)
    {
        var idRol = await _db.Roles
            .Where(r => r.Nombre == nombreRol)
            .Select(r => r.Id)
            .FirstAsync(ct);

        var yaAsignado = await _db.UsuarioGimnasioRoles
            .AnyAsync(u => u.IdUsuario == idUsuario && u.IdGimnasio == idGimnasio && u.IdRol == idRol, ct);

        if (yaAsignado) return;

        _db.UsuarioGimnasioRoles.Add(new UsuarioGimnasioRol
        {
            IdUsuario = idUsuario,
            IdGimnasio = idGimnasio,
            IdRol = idRol,
            FechaDesde = _reloj.Hoy,
            Activo = true
        });
    }
}
