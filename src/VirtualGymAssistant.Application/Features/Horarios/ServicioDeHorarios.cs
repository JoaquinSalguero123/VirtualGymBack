using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Constants;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Features.Horarios.Dtos;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Entities;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.Horarios;

/// <summary>
/// Caso de uso "Configurar los horarios de la sucursal".
/// La consulta es pública (el sitio muestra los horarios sin pedir login); la edición
/// queda en manos del mostrador y de la administración.
/// </summary>
public class ServicioDeHorarios
{
    private readonly IVirtualGymDbContext _db;
    private readonly IServicioDeContexto _contexto;

    public ServicioDeHorarios(IVirtualGymDbContext db, IServicioDeContexto contexto)
    {
        _db = db;
        _contexto = contexto;
    }

    /// <summary>Horarios de una sucursal cualquiera. No requiere autenticación.</summary>
    public async Task<IReadOnlyList<HorarioDto>> ListarPublicosAsync(
        int idGimnasio, CancellationToken ct = default) =>
        await Proyectar(_db.Horarios.Where(h => h.IdGimnasio == idGimnasio)).ToListAsync(ct);

    /// <summary>Horarios de la sucursal en la que se está trabajando.</summary>
    public async Task<IReadOnlyList<HorarioDto>> ListarDeLaSucursalActivaAsync(CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        return await Proyectar(_db.Horarios.Where(h => h.IdGimnasio == idGimnasio)).ToListAsync(ct);
    }

    /// <summary>Agrega una franja horaria a la sucursal activa.</summary>
    public async Task<HorarioDto> CrearAsync(GuardarHorarioRequest request, CancellationToken ct = default)
    {
        var contexto = await ExigirPermisoAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        var (apertura, cierre) = InterpretarHoras(request);

        var horario = new Horario
        {
            IdGimnasio = idGimnasio,
            DiaSemana = request.DiaSemana,
            HoraApertura = apertura,
            HoraCierre = cierre,
            Orden = request.Orden ?? request.DiaSemana
        };

        _db.Horarios.Add(horario);
        await _db.SaveChangesAsync(ct);

        return await ObtenerDtoAsync(horario.Id, ct);
    }

    /// <summary>Modifica una franja horaria de la sucursal activa.</summary>
    public async Task<HorarioDto> ActualizarAsync(
        int id, GuardarHorarioRequest request, CancellationToken ct = default)
    {
        var contexto = await ExigirPermisoAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        var horario = await _db.Horarios
            .FirstOrDefaultAsync(h => h.Id == id && h.IdGimnasio == idGimnasio, ct)
            ?? throw new NoEncontradoException("el horario", id);

        var (apertura, cierre) = InterpretarHoras(request);

        horario.DiaSemana = request.DiaSemana;
        horario.HoraApertura = apertura;
        horario.HoraCierre = cierre;
        horario.Orden = request.Orden ?? request.DiaSemana;

        await _db.SaveChangesAsync(ct);

        return await ObtenerDtoAsync(id, ct);
    }

    /// <summary>Elimina una franja horaria.</summary>
    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        var contexto = await ExigirPermisoAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        var horario = await _db.Horarios
            .FirstOrDefaultAsync(h => h.Id == id && h.IdGimnasio == idGimnasio, ct)
            ?? throw new NoEncontradoException("el horario", id);

        _db.Horarios.Remove(horario);
        await _db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------

    private async Task<Common.Models.ContextoDeUsuario> ExigirPermisoAsync(CancellationToken ct)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Recepcionista, RolesDelSistema.Administrador);

        return contexto;
    }

    /// <summary>Convierte las horas de texto a TimeOnly y valida que el cierre sea posterior.</summary>
    private static (TimeOnly Apertura, TimeOnly Cierre) InterpretarHoras(GuardarHorarioRequest request)
    {
        if (!TimeOnly.TryParse(request.Apertura, out var apertura))
            throw new ValidacionException(nameof(request.Apertura), "La hora de apertura no es válida.");

        if (!TimeOnly.TryParse(request.Cierre, out var cierre))
            throw new ValidacionException(nameof(request.Cierre), "La hora de cierre no es válida.");

        if (cierre <= apertura)
            throw new ReglaDeNegocioException("La hora de cierre tiene que ser posterior a la de apertura.");

        return (apertura, cierre);
    }

    private async Task<HorarioDto> ObtenerDtoAsync(int id, CancellationToken ct) =>
        await Proyectar(_db.Horarios.Where(h => h.Id == id)).FirstAsync(ct);

    private static IQueryable<HorarioDto> Proyectar(IQueryable<Horario> consulta) =>
        consulta
            .OrderBy(h => h.Orden ?? h.DiaSemana)
            .ThenBy(h => h.DiaSemana)
            .Select(h => new HorarioDto
            {
                Id = h.Id,
                IdGimnasio = h.IdGimnasio,
                DiaSemana = h.DiaSemana,
                Dia = DiasDeLaSemana.Nombres[h.DiaSemana - 1],
                Apertura = h.HoraApertura.ToString("HH\\:mm"),
                Cierre = h.HoraCierre.ToString("HH\\:mm"),
                Orden = h.Orden
            });
}
