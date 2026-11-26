using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Extensions;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Features.Rutinas.Dtos;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Entities;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.Rutinas;

/// <summary>
/// Caso de uso "Administrar plantillas de rutinas".
///
/// Una plantilla con organización nula es global (viene con el sistema y la ven todas las
/// organizaciones, pero nadie la edita); las que crea un gimnasio quedan atadas a su
/// organización y sólo esa organización puede tocarlas.
/// </summary>
public class ServicioDePlantillasDeRutina
{
    private readonly IVirtualGymDbContext _db;
    private readonly IServicioDeContexto _contexto;

    public ServicioDePlantillasDeRutina(IVirtualGymDbContext db, IServicioDeContexto contexto)
    {
        _db = db;
        _contexto = contexto;
    }

    /// <summary>Plantillas visibles: las globales más las de la organización activa.</summary>
    public async Task<IReadOnlyList<PlantillaDeRutinaDto>> ListarAsync(
        int? idObjetivo = null, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idOrganizacion = contexto.OrganizacionActivaRequerida;

        var consulta = _db.Rutinas
            .Where(r => r.Activa && (r.IdOrganizacion == null || r.IdOrganizacion == idOrganizacion));

        if (idObjetivo is { } objetivo)
            consulta = consulta.Where(r => r.IdObjetivo == objetivo);

        return await consulta
            .OrderBy(r => r.Nombre)
            .Select(r => new PlantillaDeRutinaDto
            {
                Id = r.Id,
                IdOrganizacion = r.IdOrganizacion,
                EsGlobal = r.IdOrganizacion == null,
                Nombre = r.Nombre,
                Descripcion = r.Descripcion,
                Intensidad = r.Intensidad,
                IdObjetivo = r.IdObjetivo,
                Objetivo = r.Objetivo != null ? r.Objetivo.Nombre : null,
                DiasPorSemana = r.DiasPorSemana,
                EjerciciosPorDia = r.EjerciciosPorDia,
                Activa = r.Activa
            })
            .ToListAsync(ct);
    }

    /// <summary>Plantilla con su detalle día por día.</summary>
    public async Task<PlantillaDeRutinaDto> ObtenerAsync(int id, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idOrganizacion = contexto.OrganizacionActivaRequerida;

        var plantilla = await _db.Rutinas
            .Where(r => r.Id == id && (r.IdOrganizacion == null || r.IdOrganizacion == idOrganizacion))
            .Select(r => new
            {
                r.Id,
                r.IdOrganizacion,
                r.Nombre,
                r.Descripcion,
                r.Intensidad,
                r.IdObjetivo,
                Objetivo = r.Objetivo != null ? r.Objetivo.Nombre : null,
                r.DiasPorSemana,
                r.EjerciciosPorDia,
                r.Activa
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NoEncontradoException("la plantilla de rutina", id);

        var detalle = await _db.RutinaDetalles
            .Where(d => d.IdRutina == id)
            .OrderBy(d => d.Dia)
            .ThenBy(d => d.Orden)
            .Select(d => new
            {
                d.Dia,
                Ejercicio = new EjercicioDeRutinaDto
                {
                    Orden = d.Orden,
                    IdEjercicio = d.IdEjercicio,
                    Ejercicio = d.Ejercicio.Nombre,
                    Grupo = d.Ejercicio.Grupo.Nombre,
                    Series = d.Series,
                    Repeticiones = d.Repeticiones,
                    DescansoSeg = d.DescansoSeg,
                    VideoUrl = d.Ejercicio.VideoUrl
                }
            })
            .ToListAsync(ct);

        return new PlantillaDeRutinaDto
        {
            Id = plantilla.Id,
            IdOrganizacion = plantilla.IdOrganizacion,
            EsGlobal = plantilla.IdOrganizacion is null,
            Nombre = plantilla.Nombre,
            Descripcion = plantilla.Descripcion,
            Intensidad = plantilla.Intensidad,
            IdObjetivo = plantilla.IdObjetivo,
            Objetivo = plantilla.Objetivo,
            DiasPorSemana = plantilla.DiasPorSemana,
            EjerciciosPorDia = plantilla.EjerciciosPorDia,
            Activa = plantilla.Activa,
            Dias = detalle
                .GroupBy(d => d.Dia)
                .OrderBy(g => g.Key)
                .Select(g => new DiaDeRutinaDto
                {
                    Dia = g.Key,
                    Ejercicios = g.Select(x => x.Ejercicio).ToList()
                })
                .ToList()
        };
    }

    /// <summary>Crea una plantilla dentro de la organización activa.</summary>
    public async Task<PlantillaDeRutinaDto> CrearAsync(
        GuardarPlantillaRequest request, CancellationToken ct = default)
    {
        var contexto = await ExigirPermisoAsync(ct);
        var idOrganizacion = contexto.OrganizacionActivaRequerida;

        var nombre = request.Nombre.Trim();

        if (await _db.Rutinas.AnyAsync(r => r.IdOrganizacion == idOrganizacion && r.Nombre == nombre, ct))
            throw new ConflictoException($"Ya existe una plantilla llamada \"{nombre}\".");

        await ExigirObjetivoValidoAsync(request.IdObjetivo, ct);

        var plantilla = new Rutina
        {
            IdOrganizacion = idOrganizacion,
            Nombre = nombre,
            Descripcion = request.Descripcion.NormalizarONull(),
            Intensidad = request.Intensidad.NormalizarONull(),
            IdObjetivo = request.IdObjetivo,
            DiasPorSemana = request.DiasPorSemana,
            EjerciciosPorDia = request.EjerciciosPorDia,
            Activa = true
        };

        _db.Rutinas.Add(plantilla);
        await _db.SaveChangesAsync(ct);

        await ReemplazarDetalleAsync(plantilla.Id, request.Dias, ct);
        await _db.SaveChangesAsync(ct);

        return await ObtenerAsync(plantilla.Id, ct);
    }

    /// <summary>Modifica una plantilla propia, reemplazando su detalle completo.</summary>
    public async Task<PlantillaDeRutinaDto> ActualizarAsync(
        int id, GuardarPlantillaRequest request, CancellationToken ct = default)
    {
        var contexto = await ExigirPermisoAsync(ct);
        var idOrganizacion = contexto.OrganizacionActivaRequerida;

        var plantilla = await ExigirPlantillaPropiaAsync(id, idOrganizacion, ct);
        var nombre = request.Nombre.Trim();

        if (await _db.Rutinas.AnyAsync(
                r => r.IdOrganizacion == idOrganizacion && r.Nombre == nombre && r.Id != id, ct))
            throw new ConflictoException($"Ya existe otra plantilla llamada \"{nombre}\".");

        await ExigirObjetivoValidoAsync(request.IdObjetivo, ct);

        plantilla.Nombre = nombre;
        plantilla.Descripcion = request.Descripcion.NormalizarONull();
        plantilla.Intensidad = request.Intensidad.NormalizarONull();
        plantilla.IdObjetivo = request.IdObjetivo;
        plantilla.DiasPorSemana = request.DiasPorSemana;
        plantilla.EjerciciosPorDia = request.EjerciciosPorDia;

        await ReemplazarDetalleAsync(id, request.Dias, ct);
        await _db.SaveChangesAsync(ct);

        return await ObtenerAsync(id, ct);
    }

    /// <summary>
    /// Baja lógica de la plantilla: deja de ofrecerse, pero las rutinas que ya salieron
    /// de ella siguen funcionando.
    /// </summary>
    public async Task DarDeBajaAsync(int id, CancellationToken ct = default)
    {
        var contexto = await ExigirPermisoAsync(ct);
        var plantilla = await ExigirPlantillaPropiaAsync(id, contexto.OrganizacionActivaRequerida, ct);

        plantilla.Activa = false;
        await _db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------

    private async Task<Common.Models.ContextoDeUsuario> ExigirPermisoAsync(CancellationToken ct)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Entrenador, RolesDelSistema.Administrador);

        return contexto;
    }

    /// <summary>
    /// Trae la plantilla verificando que sea de la organización activa.
    /// Las globales se pueden usar pero no editar: son del sistema.
    /// </summary>
    private async Task<Rutina> ExigirPlantillaPropiaAsync(int id, int idOrganizacion, CancellationToken ct)
    {
        var plantilla = await _db.Rutinas.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NoEncontradoException("la plantilla de rutina", id);

        if (plantilla.IdOrganizacion is null)
            throw new AccesoDenegadoException(
                "Las plantillas globales del sistema no se pueden modificar. " +
                "Duplicala como plantilla propia si querés adaptarla.");

        if (plantilla.IdOrganizacion != idOrganizacion)
            throw new AccesoDenegadoException("La plantilla pertenece a otra organización.");

        return plantilla;
    }

    private async Task ExigirObjetivoValidoAsync(int? idObjetivo, CancellationToken ct)
    {
        if (idObjetivo is { } objetivo && !await _db.Objetivos.AnyAsync(o => o.Id == objetivo, ct))
            throw new NoEncontradoException("el objetivo", objetivo);
    }

    /// <summary>
    /// Borra el detalle anterior y lo vuelve a cargar. Reemplazar es más simple y seguro
    /// que intentar sincronizar renglón por renglón, y el volumen de una rutina lo permite.
    /// </summary>
    private async Task ReemplazarDetalleAsync(int idRutina, List<DiaDeRutinaRequest> dias, CancellationToken ct)
    {
        var anteriores = await _db.RutinaDetalles.Where(d => d.IdRutina == idRutina).ToListAsync(ct);
        _db.RutinaDetalles.RemoveRange(anteriores);

        var idsDeEjercicios = dias.SelectMany(d => d.Ejercicios).Select(e => e.IdEjercicio).Distinct().ToList();

        var existentes = await _db.Ejercicios
            .Where(e => idsDeEjercicios.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync(ct);

        var faltante = idsDeEjercicios.FirstOrDefault(id => !existentes.Contains(id));
        if (faltante != 0)
            throw new NoEncontradoException("el ejercicio", faltante);

        foreach (var dia in dias)
        {
            var orden = 1;

            foreach (var ejercicio in dia.Ejercicios)
            {
                _db.RutinaDetalles.Add(new RutinaDetalle
                {
                    IdRutina = idRutina,
                    Dia = (byte)dia.Dia,
                    Orden = orden++,
                    IdEjercicio = ejercicio.IdEjercicio,
                    Series = ejercicio.Series,
                    Repeticiones = ejercicio.Repeticiones.NormalizarONull(),
                    DescansoSeg = ejercicio.DescansoSeg
                });
            }
        }
    }
}
