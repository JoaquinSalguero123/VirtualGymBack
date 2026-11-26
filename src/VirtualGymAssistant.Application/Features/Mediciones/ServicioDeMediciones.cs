using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Extensions;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Features.Mediciones.Dtos;
using VirtualGymAssistant.Application.Features.Socios;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Entities;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.Mediciones;

/// <summary>
/// Seguimiento físico del socio. Las mediciones las puede cargar el personal técnico
/// (entrenador, nutricionista o administración) o el propio socio, sobre sí mismo.
/// </summary>
public class ServicioDeMediciones
{
    private readonly IVirtualGymDbContext _db;
    private readonly IServicioDeContexto _contexto;
    private readonly IProveedorDeFechaHora _reloj;
    private readonly GestorDeFichasDeSocio _fichasDeSocio;

    public ServicioDeMediciones(
        IVirtualGymDbContext db,
        IServicioDeContexto contexto,
        IProveedorDeFechaHora reloj,
        GestorDeFichasDeSocio fichasDeSocio)
    {
        _db = db;
        _contexto = contexto;
        _reloj = reloj;
        _fichasDeSocio = fichasDeSocio;
    }

    /// <summary>Historial de mediciones de un socio, de la más reciente a la más vieja.</summary>
    public async Task<IReadOnlyList<MedicionDto>> ListarDeSocioAsync(
        int idSocio, CancellationToken ct = default)
    {
        await ExigirAccesoAlSocioAsync(idSocio, ct);

        // Fecha es un DateOnly: dos mediciones cargadas el mismo día empatan ahí, así
        // que se desempata por Id para que la última cargada quede primera.
        return await Proyectar(_db.Mediciones.Where(m => m.IdSocio == idSocio))
            .OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id)
            .ToListAsync(ct);
    }

    /// <summary>Mediciones del socio autenticado en la sucursal activa.</summary>
    public async Task<IReadOnlyList<MedicionDto>> ListarMisMedicionesAsync(CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        var idSocio = await _fichasDeSocio.BuscarIdAsync(contexto.IdUsuario, idGimnasio, ct);
        if (idSocio is null) return [];

        return await Proyectar(_db.Mediciones.Where(m => m.IdSocio == idSocio))
            .OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id)
            .ToListAsync(ct);
    }

    /// <summary>Compara la primera contra la última medición para mostrar la evolución.</summary>
    public async Task<ProgresoDelSocioDto> ObtenerProgresoAsync(
        int idSocio, CancellationToken ct = default)
    {
        await ExigirAccesoAlSocioAsync(idSocio, ct);

        var mediciones = await Proyectar(_db.Mediciones.Where(m => m.IdSocio == idSocio))
            .OrderBy(m => m.Fecha).ThenBy(m => m.Id)
            .ToListAsync(ct);

        var primera = mediciones.FirstOrDefault();
        var ultima = mediciones.LastOrDefault();

        return new ProgresoDelSocioDto
        {
            IdSocio = idSocio,
            PrimeraMedicion = primera,
            UltimaMedicion = ultima,
            CantidadDeMediciones = mediciones.Count,
            DiferenciaDePeso = primera is not null && ultima is not null
                ? ultima.Peso - primera.Peso
                : null,
            DiferenciaDeGrasa = primera?.GrasaPct is not null && ultima?.GrasaPct is not null
                ? ultima.GrasaPct - primera.GrasaPct
                : null,
            DiferenciaDeMusculo = primera?.MusculoPct is not null && ultima?.MusculoPct is not null
                ? ultima.MusculoPct - primera.MusculoPct
                : null
        };
    }

    /// <summary>
    /// Registra una medición. La puede cargar el entrenador, el nutricionista o la
    /// administración para cualquier socio de la sucursal, o el propio socio para sí
    /// mismo: llevar el propio registro entre visita y visita no debería depender de
    /// que alguien más lo cargue.
    /// </summary>
    public async Task<MedicionDto> CrearAsync(GuardarMedicionRequest request, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        var socio = await _db.Socios
            .FirstOrDefaultAsync(s => s.Id == request.IdSocio && s.IdGimnasio == idGimnasio, ct)
            ?? throw new NoEncontradoException("el socio", request.IdSocio);

        if (socio.IdUsuario != contexto.IdUsuario)
            contexto.ExigirRol(RolesDelSistema.Entrenador, RolesDelSistema.Nutricionista,
                               RolesDelSistema.Administrador);

        var fecha = request.Fecha ?? _reloj.Hoy;

        if (fecha > _reloj.Hoy)
            throw new ReglaDeNegocioException("No se pueden cargar mediciones con fecha futura.");

        var medicion = new Medicion
        {
            IdSocio = request.IdSocio,
            Fecha = fecha,
            Peso = request.Peso,
            GrasaPct = request.GrasaPct,
            MusculoPct = request.MusculoPct,
            Cintura = request.Cintura,
            Cadera = request.Cadera,
            Pecho = request.Pecho,
            Brazo = request.Brazo,
            Pierna = request.Pierna,
            IdRegistradoPor = contexto.IdUsuario,
            Observaciones = request.Observaciones.NormalizarONull()
        };

        _db.Mediciones.Add(medicion);
        await _db.SaveChangesAsync(ct);

        return await Proyectar(_db.Mediciones.Where(m => m.Id == medicion.Id)).FirstAsync(ct);
    }

    /// <summary>Elimina una medición cargada por error.</summary>
    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Entrenador, RolesDelSistema.Nutricionista,
                           RolesDelSistema.Administrador);

        var idGimnasio = contexto.GimnasioActivoRequerido;

        var medicion = await _db.Mediciones
            .FirstOrDefaultAsync(m => m.Id == id && m.Socio.IdGimnasio == idGimnasio, ct)
            ?? throw new NoEncontradoException("la medición", id);

        _db.Mediciones.Remove(medicion);
        await _db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------

    /// <summary>
    /// Un socio puede ver sus propias mediciones; para ver las de otro hay que ser
    /// personal técnico o administración de esa sucursal.
    /// </summary>
    private async Task ExigirAccesoAlSocioAsync(int idSocio, CancellationToken ct)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        var socio = await _db.Socios
            .FirstOrDefaultAsync(s => s.Id == idSocio && s.IdGimnasio == idGimnasio, ct)
            ?? throw new NoEncontradoException("el socio", idSocio);

        if (socio.IdUsuario == contexto.IdUsuario) return;

        contexto.ExigirRol(RolesDelSistema.Entrenador, RolesDelSistema.Nutricionista,
                           RolesDelSistema.Administrador, RolesDelSistema.Recepcionista);
    }

    private static IQueryable<MedicionDto> Proyectar(IQueryable<Medicion> consulta) =>
        consulta.Select(m => new MedicionDto
        {
            Id = m.Id,
            IdSocio = m.IdSocio,
            Socio = (m.Socio.Usuario.Nombre + " " + m.Socio.Usuario.Apellido).Trim(),
            Fecha = m.Fecha,
            Peso = m.Peso,
            GrasaPct = m.GrasaPct,
            MusculoPct = m.MusculoPct,
            Cintura = m.Cintura,
            Cadera = m.Cadera,
            Pecho = m.Pecho,
            Brazo = m.Brazo,
            Pierna = m.Pierna,
            IdRegistradoPor = m.IdRegistradoPor,
            RegistradoPor = m.RegistradoPor != null
                ? (m.RegistradoPor.Nombre + " " + m.RegistradoPor.Apellido).Trim()
                : null,
            Observaciones = m.Observaciones
        });
}
