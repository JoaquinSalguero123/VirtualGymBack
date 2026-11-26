using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Extensions;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Common.Models;
using VirtualGymAssistant.Application.Features.Autenticacion;
using VirtualGymAssistant.Application.Features.Socios;
using VirtualGymAssistant.Application.Features.Usuarios.Dtos;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Entities;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.Usuarios;

/// <summary>
/// Casos de uso de "Personal y roles": alta de empleados en una sucursal, cambio de rol,
/// asignación en varias sedes y baja de la asignación.
///
/// Todo lo que modifica personal exige rol Administrador (o ser el dueño).
/// </summary>
public class ServicioDeUsuarios
{
    private readonly IVirtualGymDbContext _db;
    private readonly IServicioDeContexto _contexto;
    private readonly IHasheadorDeClaves _hasheador;
    private readonly IProveedorDeFechaHora _reloj;
    private readonly GestorDeFichasDeSocio _fichasDeSocio;
    private readonly GestorDeVerificacionDeEmail _verificacion;

    public ServicioDeUsuarios(
        IVirtualGymDbContext db,
        IServicioDeContexto contexto,
        IHasheadorDeClaves hasheador,
        IProveedorDeFechaHora reloj,
        GestorDeFichasDeSocio fichasDeSocio,
        GestorDeVerificacionDeEmail verificacion)
    {
        _db = db;
        _contexto = contexto;
        _hasheador = hasheador;
        _reloj = reloj;
        _fichasDeSocio = fichasDeSocio;
        _verificacion = verificacion;
    }

    /// <summary>Catálogo de roles disponibles.</summary>
    public async Task<IReadOnlyList<RolDto>> ListarRolesAsync(CancellationToken ct = default) =>
        await _db.Roles
            .OrderBy(r => r.Id)
            .Select(r => new RolDto { Id = r.Id, Nombre = r.Nombre, Descripcion = r.Descripcion })
            .ToListAsync(ct);

    /// <summary>
    /// Personal asignado a la sucursal activa. Excluye a los socios: para verlos está el padrón.
    /// </summary>
    public async Task<ResultadoPaginado<UsuarioDto>> ListarPersonalAsync(
        string? texto,
        ParametrosDePaginacion paginacion,
        CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Recepcionista, RolesDelSistema.Administrador);

        var idGimnasio = contexto.GimnasioActivoRequerido;

        var consulta = _db.UsuarioGimnasioRoles
            .Where(ugr => ugr.IdGimnasio == idGimnasio
                          && ugr.Activo
                          && ugr.Rol.Nombre != RolesDelSistema.Socio);

        if (texto.NormalizarONull() is { } filtro)
        {
            consulta = consulta.Where(ugr =>
                ugr.Usuario.Nombre!.Contains(filtro) ||
                ugr.Usuario.Apellido!.Contains(filtro) ||
                ugr.Usuario.Mail.Contains(filtro) ||
                ugr.Usuario.Username.Contains(filtro));
        }

        var proyeccion = consulta
            .OrderBy(ugr => ugr.Usuario.Apellido)
            .ThenBy(ugr => ugr.Usuario.Nombre)
            .Select(ugr => new UsuarioDto
            {
                Id = ugr.Usuario.Id,
                Username = ugr.Usuario.Username,
                Nombre = ugr.Usuario.Nombre,
                Apellido = ugr.Usuario.Apellido,
                Email = ugr.Usuario.Mail,
                Telefono = ugr.Usuario.Telefono,
                Activo = ugr.Usuario.Activo,
                FechaAlta = ugr.Usuario.FechaAlta,
                IdRol = ugr.IdRol,
                Rol = ugr.Rol.Nombre
            });

        return await proyeccion.PaginarAsync(paginacion, ct);
    }

    /// <summary>Detalle de un usuario que opera en la sucursal activa.</summary>
    public async Task<UsuarioDto> ObtenerAsync(int idUsuario, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Recepcionista, RolesDelSistema.Administrador);

        var idGimnasio = contexto.GimnasioActivoRequerido;

        return await _db.Usuarios
            .Where(u => u.Id == idUsuario)
            .Select(u => new UsuarioDto
            {
                Id = u.Id,
                Username = u.Username,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Mail,
                Telefono = u.Telefono,
                Activo = u.Activo,
                FechaAlta = u.FechaAlta,
                IdRol = u.RolesPorSucursal
                    .Where(r => r.IdGimnasio == idGimnasio && r.Activo)
                    .Select(r => (int?)r.IdRol)
                    .FirstOrDefault(),
                Rol = u.RolesPorSucursal
                    .Where(r => r.IdGimnasio == idGimnasio && r.Activo)
                    .Select(r => r.Rol.Nombre)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NoEncontradoException("el usuario", idUsuario);
    }

    /// <summary>Todas las sucursales en las que un usuario tiene rol asignado.</summary>
    public async Task<IReadOnlyList<AsignacionDeRolDto>> ListarAsignacionesAsync(
        int idUsuario, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Administrador);

        // Un administrador sólo puede ver asignaciones dentro de su propia organización.
        var idOrganizacion = contexto.OrganizacionActivaRequerida;

        return await _db.UsuarioGimnasioRoles
            .Where(ugr => ugr.IdUsuario == idUsuario && ugr.Gimnasio.IdOrganizacion == idOrganizacion)
            .OrderBy(ugr => ugr.Gimnasio.Nombre)
            .Select(ugr => new AsignacionDeRolDto
            {
                IdAsignacion = ugr.Id,
                IdGimnasio = ugr.IdGimnasio,
                Sucursal = ugr.Gimnasio.Nombre,
                IdRol = ugr.IdRol,
                Rol = ugr.Rol.Nombre,
                FechaDesde = ugr.FechaDesde,
                FechaHasta = ugr.FechaHasta,
                Activo = ugr.Activo
            })
            .ToListAsync(ct);
    }

    /// <summary>
    /// Caso de uso "Dar de alta personal en una sucursal".
    /// Crea el usuario y le asigna el rol elegido en la sucursal activa. Si el rol es
    /// Socio, además le arma la ficha de socio.
    ///
    /// La cuenta nace con el email sin verificar, igual que en el registro propio: la da de
    /// alta la administración, pero nadie comprobó que ese mail sea de esa persona. Por eso se
    /// le manda el código en el acto y, la primera vez que ingrese, tiene que confirmarlo.
    /// </summary>
    public async Task<UsuarioDto> CrearAsync(CrearUsuarioRequest request, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Administrador);

        var idGimnasio = contexto.GimnasioActivoRequerido;
        var email = request.Email.Trim().ToLowerInvariant();
        var username = (request.Username.NormalizarONull() ?? email.Split('@')[0]).ASlugDeUsuario();

        if (await _db.Usuarios.AnyAsync(u => u.Mail == email, ct))
            throw new ConflictoException("Ya existe un usuario con ese email.");

        if (await _db.Usuarios.AnyAsync(u => u.Username == username, ct))
            throw new ConflictoException($"Ya existe un usuario con el nombre de usuario \"{username}\".");

        var telefono = request.Telefono.NormalizarONull();
        if (telefono is not null && await _db.Usuarios.AnyAsync(u => u.Telefono == telefono, ct))
            throw new ConflictoException("Ya existe un usuario con ese teléfono.");

        var rol = await _db.Roles.FirstOrDefaultAsync(r => r.Id == request.IdRol, ct)
            ?? throw new NoEncontradoException("el rol", request.IdRol);

        ExigirPoderAsignarRol(contexto, rol.Nombre);

        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(),
            Apellido = request.Apellido.Trim(),
            Mail = email,
            Telefono = telefono,
            Username = username,
            Clave = _hasheador.Hashear(request.Contrasena),
            Activo = true,
            FechaAlta = _reloj.Ahora
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync(ct);

        _db.UsuarioGimnasioRoles.Add(new UsuarioGimnasioRol
        {
            IdUsuario = usuario.Id,
            IdGimnasio = idGimnasio,
            IdRol = rol.Id,
            FechaDesde = _reloj.Hoy,
            Activo = true
        });
        await _db.SaveChangesAsync(ct);

        if (rol.Nombre == RolesDelSistema.Socio)
            await _fichasDeSocio.ObtenerOCrearAsync(usuario.Id, idGimnasio, ct);

        await _verificacion.EnviarCodigoAsync(usuario, ct);

        return await ObtenerAsync(usuario.Id, ct);
    }

    /// <summary>
    /// Caso de uso "Cambiar el rol de un usuario en una sucursal", junto con la edición
    /// de sus datos personales.
    /// </summary>
    public async Task<UsuarioDto> ActualizarAsync(
        int idUsuario, ActualizarUsuarioRequest request, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Administrador);

        var idGimnasio = contexto.GimnasioActivoRequerido;

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == idUsuario, ct)
            ?? throw new NoEncontradoException("el usuario", idUsuario);

        await ExigirQueOpereEnLaSucursalAsync(idUsuario, idGimnasio, ct);

        var username = request.Username.NormalizarONull()?.ASlugDeUsuario() ?? usuario.Username;

        if (username != usuario.Username &&
            await _db.Usuarios.AnyAsync(u => u.Username == username && u.Id != idUsuario, ct))
            throw new ConflictoException($"Ya existe otro usuario con el nombre \"{username}\".");

        var telefono = request.Telefono.NormalizarONull();
        if (telefono is not null &&
            await _db.Usuarios.AnyAsync(u => u.Telefono == telefono && u.Id != idUsuario, ct))
            throw new ConflictoException("Ya existe otro usuario con ese teléfono.");

        usuario.Nombre = request.Nombre.Trim();
        usuario.Apellido = request.Apellido.Trim();
        usuario.Telefono = telefono;
        usuario.Username = username;
        usuario.Activo = request.Activo;

        if (request.IdRol is { } idRol)
        {
            var rol = await _db.Roles.FirstOrDefaultAsync(r => r.Id == idRol, ct)
                ?? throw new NoEncontradoException("el rol", idRol);

            // El formulario de edición manda siempre el rol, aunque no se haya tocado.
            // Volver a guardar el que ya tiene no es designar a nadie, así que las
            // reglas de asignación sólo se aplican cuando el rol realmente cambia.
            var yaTieneEseRol = await _db.UsuarioGimnasioRoles.AnyAsync(
                ugr => ugr.IdUsuario == idUsuario && ugr.IdGimnasio == idGimnasio &&
                       ugr.IdRol == idRol && ugr.Activo, ct);

            if (!yaTieneEseRol)
            {
                // Se valida también acá: si no, un administrador podría promover a otro
                // administrador desde la edición, salteándose la regla del alta.
                ExigirPoderAsignarRol(contexto, rol.Nombre);

                await ReemplazarRolEnSucursalAsync(idUsuario, idGimnasio, idRol, ct);
            }
        }

        await _db.SaveChangesAsync(ct);

        return await ObtenerAsync(idUsuario, ct);
    }

    /// <summary>
    /// Caso de uso "Asignar roles en varias sucursales": habilita al mismo empleado en
    /// otra sede de la organización.
    /// </summary>
    public async Task AsignarEnSucursalAsync(
        int idUsuario, AsignarRolRequest request, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Administrador);

        // El administrador sólo puede asignar en sucursales sobre las que él mismo opera.
        contexto.ExigirAccesoA(request.IdGimnasio);

        if (!await _db.Usuarios.AnyAsync(u => u.Id == idUsuario, ct))
            throw new NoEncontradoException("el usuario", idUsuario);

        var rol = await _db.Roles.FirstOrDefaultAsync(r => r.Id == request.IdRol, ct)
            ?? throw new NoEncontradoException("el rol", request.IdRol);

        ExigirPoderAsignarRol(contexto, rol.Nombre);

        await ReemplazarRolEnSucursalAsync(idUsuario, request.IdGimnasio, rol.Id, ct);
        await _db.SaveChangesAsync(ct);

        if (rol.Nombre == RolesDelSistema.Socio)
            await _fichasDeSocio.ObtenerOCrearAsync(idUsuario, request.IdGimnasio, ct);
    }

    /// <summary>
    /// Caso de uso "Quitar a un usuario de una sucursal".
    /// Es una baja lógica de la asignación: el usuario conserva su cuenta y sus roles en
    /// las demás sedes.
    /// </summary>
    public async Task QuitarDeSucursalAsync(int idUsuario, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Administrador);

        var idGimnasio = contexto.GimnasioActivoRequerido;

        if (idUsuario == contexto.IdUsuario)
            throw new ReglaDeNegocioException("No podés quitarte a vos mismo de la sucursal.");

        var asignaciones = await _db.UsuarioGimnasioRoles
            .Where(ugr => ugr.IdUsuario == idUsuario && ugr.IdGimnasio == idGimnasio && ugr.Activo)
            .ToListAsync(ct);

        if (asignaciones.Count == 0)
            throw new NoEncontradoException("El usuario no tiene ningún rol activo en esta sucursal.");

        foreach (var asignacion in asignaciones)
        {
            asignacion.Activo = false;
            asignacion.FechaHasta = _reloj.Hoy;
        }

        await _db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------

    /// <summary>
    /// Desactiva los roles que el usuario tenga en la sucursal y deja activo sólo el nuevo.
    /// Si esa combinación ya existía (aunque estuviera dada de baja) se reutiliza la fila,
    /// porque la base tiene una restricción única por usuario + sucursal + rol.
    /// </summary>
    private async Task ReemplazarRolEnSucursalAsync(
        int idUsuario,
        int idGimnasio,
        int idRol,
        CancellationToken ct)
    {
        var existentes = await _db.UsuarioGimnasioRoles
            .Where(ugr => ugr.IdUsuario == idUsuario && ugr.IdGimnasio == idGimnasio)
            .ToListAsync(ct);

        foreach (var asignacion in existentes)
        {
            var esLaQueQueremos = asignacion.IdRol == idRol;
            asignacion.Activo = esLaQueQueremos;
            asignacion.FechaHasta = esLaQueQueremos ? null : _reloj.Hoy;
        }

        if (existentes.All(a => a.IdRol != idRol))
        {
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

    /// <summary>Comprueba que el usuario tenga alguna asignación en esa sucursal.</summary>
    private async Task ExigirQueOpereEnLaSucursalAsync(int idUsuario, int idGimnasio, CancellationToken ct)
    {
        var opera = await _db.UsuarioGimnasioRoles
            .AnyAsync(ugr => ugr.IdUsuario == idUsuario && ugr.IdGimnasio == idGimnasio, ct);

        if (!opera)
            throw new AccesoDenegadoException(
                "El usuario no pertenece a la sucursal activa, así que no se puede editar desde acá.");
    }

    /// <summary>
    /// El rol Dueño no se asigna a mano: surge de ser titular de la organización.
    /// Y sólo el dueño puede crear otros administradores.
    /// </summary>
    private static void ExigirPoderAsignarRol(Common.Models.ContextoDeUsuario contexto, string nombreRol)
    {
        if (nombreRol == RolesDelSistema.Duenio)
            throw new ReglaDeNegocioException(
                "El rol Dueño no se asigna manualmente: lo determina la titularidad de la organización.");

        if (nombreRol == RolesDelSistema.Administrador && !contexto.EsDuenio)
            throw new AccesoDenegadoException(
                "Sólo el dueño de la organización puede designar administradores.");
    }
}
