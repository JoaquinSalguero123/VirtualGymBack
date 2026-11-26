using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Extensions;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Features.Rutinas.Dtos;
using VirtualGymAssistant.Application.Features.Socios;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Entities;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.Rutinas;

/// <summary>
/// Casos de uso "Generar y guardar una rutina" y "Consultar rutina asignada".
///
/// Una rutina se puede armar de tres formas:
///   1. Copiando una plantilla.
///   2. Dejando que el sistema la genere según un objetivo, repartiendo los ejercicios
///      entre los distintos grupos musculares.
///   3. A mano, día por día, desde el front.
///
/// Regla central: el socio tiene una sola rutina activa. Al guardar una nueva, la anterior
/// se archiva (queda inactiva) pero no se borra.
/// </summary>
public class ServicioDeRutinasDeSocio
{
    private readonly IVirtualGymDbContext _db;
    private readonly IServicioDeContexto _contexto;
    private readonly IProveedorDeFechaHora _reloj;
    private readonly GestorDeFichasDeSocio _fichasDeSocio;

    public ServicioDeRutinasDeSocio(
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

    /// <summary>
    /// Arma una propuesta de rutina SIN guardarla, para que el socio la revise antes de
    /// confirmarla.
    /// </summary>
    public async Task<IReadOnlyList<DiaDeRutinaDto>> GenerarPropuestaAsync(
        GenerarRutinaRequest request, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idOrganizacion = contexto.OrganizacionActivaRequerida;

        // Si pidieron una plantilla puntual, o hay una que coincide con el objetivo,
        // se copia su detalle: siempre es mejor una rutina armada por un entrenador.
        var plantilla = await BuscarPlantillaAsync(request, idOrganizacion, ct);

        if (plantilla is not null)
            return await CopiarDetalleDePlantillaAsync(plantilla.Id, ct);

        return await ArmarRutinaAutomaticaAsync(request, ct);
    }

    /// <summary>Rutina vigente del socio autenticado en la sucursal activa.</summary>
    public async Task<RutinaDeSocioDto?> ObtenerMiRutinaAsync(CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        var idSocio = await _fichasDeSocio.BuscarIdAsync(contexto.IdUsuario, idGimnasio, ct);
        if (idSocio is null) return null;

        return await ObtenerRutinaVigenteAsync(idSocio.Value, ct);
    }

    /// <summary>
    /// Caso de uso "Consultar la rutina de un socio", para el entrenador.
    /// </summary>
    public async Task<RutinaDeSocioDto?> ObtenerDeSocioAsync(int idSocio, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        var socio = await _db.Socios
            .FirstOrDefaultAsync(s => s.Id == idSocio && s.IdGimnasio == idGimnasio, ct)
            ?? throw new NoEncontradoException("el socio", idSocio);

        if (socio.IdUsuario != contexto.IdUsuario)
            contexto.ExigirRol(RolesDelSistema.Entrenador, RolesDelSistema.Administrador);

        return await ObtenerRutinaVigenteAsync(idSocio, ct);
    }

    /// <summary>Historial de rutinas de un socio, de la más reciente a la más antigua.</summary>
    public async Task<IReadOnlyList<RutinaDeSocioDto>> ListarHistorialDeSocioAsync(
        int idSocio, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        var socio = await _db.Socios
            .FirstOrDefaultAsync(s => s.Id == idSocio && s.IdGimnasio == idGimnasio, ct)
            ?? throw new NoEncontradoException("el socio", idSocio);

        if (socio.IdUsuario != contexto.IdUsuario)
            contexto.ExigirRol(RolesDelSistema.Entrenador, RolesDelSistema.Administrador);

        var cabeceras = await ProyectarCabecera(_db.SocioRutinas.Where(r => r.IdSocio == idSocio))
            .OrderByDescending(r => r.Activa)
            .ThenByDescending(r => r.FechaInicio)
            .ToListAsync(ct);

        return cabeceras;
    }

    /// <summary>
    /// Caso de uso "Generar y guardar una rutina": el socio confirma su propia rutina.
    /// </summary>
    public async Task<RutinaDeSocioDto> GuardarMiRutinaAsync(
        GuardarRutinaDeSocioRequest request, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        // Regla automática del sistema: si todavía no es socio de la sede, se crea la ficha.
        var idSocio = await _fichasDeSocio.ObtenerOCrearAsync(contexto.IdUsuario, idGimnasio, ct);

        return await GuardarAsync(idSocio, request, idEntrenador: null, ct);
    }

    /// <summary>El entrenador arma y asigna una rutina a un socio de su sucursal.</summary>
    public async Task<RutinaDeSocioDto> AsignarASocioAsync(
        AsignarRutinaRequest request, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Entrenador, RolesDelSistema.Administrador);

        var idGimnasio = contexto.GimnasioActivoRequerido;

        if (!await _db.Socios.AnyAsync(s => s.Id == request.IdSocio && s.IdGimnasio == idGimnasio, ct))
            throw new NoEncontradoException("el socio", request.IdSocio);

        return await GuardarAsync(request.IdSocio, request, contexto.IdUsuario, ct);
    }

    // ------------------------------------------------------------------
    // Reglas internas
    // ------------------------------------------------------------------

    /// <summary>
    /// Archiva la rutina activa anterior y guarda la nueva con su detalle.
    /// </summary>
    private async Task<RutinaDeSocioDto> GuardarAsync(
        int idSocio, GuardarRutinaDeSocioRequest request, int? idEntrenador, CancellationToken ct)
    {
        if (request.Dias.Count == 0)
            throw new ReglaDeNegocioException("La rutina tiene que tener al menos un día con ejercicios.");

        var anteriores = await _db.SocioRutinas
            .Where(r => r.IdSocio == idSocio && r.Activa)
            .ToListAsync(ct);

        foreach (var anterior in anteriores)
        {
            anterior.Activa = false;
            anterior.FechaFin = _reloj.Hoy;
        }

        var rutina = new SocioRutina
        {
            IdSocio = idSocio,
            IdRutina = request.IdPlantilla,
            IdEntrenador = idEntrenador,
            Nombre = request.Nombre.NormalizarONull() ?? "Rutina personalizada",
            FechaInicio = _reloj.Hoy,
            Activa = true
        };

        _db.SocioRutinas.Add(rutina);
        await _db.SaveChangesAsync(ct);

        await AgregarDetalleAsync(rutina.Id, request.Dias, ct);
        await _db.SaveChangesAsync(ct);

        return (await ObtenerRutinaVigenteAsync(idSocio, ct))!;
    }

    /// <summary>Inserta el detalle validando que todos los ejercicios existan.</summary>
    private async Task AgregarDetalleAsync(int idSocioRutina, List<DiaDeRutinaRequest> dias, CancellationToken ct)
    {
        var ids = dias.SelectMany(d => d.Ejercicios).Select(e => e.IdEjercicio).Distinct().ToList();

        var existentes = await _db.Ejercicios
            .Where(e => ids.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync(ct);

        var faltante = ids.FirstOrDefault(id => !existentes.Contains(id));
        if (faltante != 0)
            throw new NoEncontradoException("el ejercicio", faltante);

        foreach (var dia in dias)
        {
            var orden = 1;

            foreach (var ejercicio in dia.Ejercicios)
            {
                _db.SocioRutinaDetalles.Add(new SocioRutinaDetalle
                {
                    IdSocioRutina = idSocioRutina,
                    Dia = dia.Dia,
                    Orden = orden++,
                    IdEjercicio = ejercicio.IdEjercicio,
                    Series = ejercicio.Series,
                    Repeticiones = ejercicio.Repeticiones.NormalizarONull(),
                    DescansoSeg = ejercicio.DescansoSeg,
                    Notas = ejercicio.Notas.NormalizarONull()
                });
            }
        }
    }

    /// <summary>Busca la plantilla pedida o, en su defecto, una que coincida con el objetivo.</summary>
    private async Task<Rutina?> BuscarPlantillaAsync(
        GenerarRutinaRequest request, int idOrganizacion, CancellationToken ct)
    {
        if (request.IdPlantilla is { } idPlantilla)
        {
            return await _db.Rutinas
                .FirstOrDefaultAsync(r => r.Id == idPlantilla
                                          && r.Activa
                                          && (r.IdOrganizacion == null || r.IdOrganizacion == idOrganizacion), ct)
                ?? throw new NoEncontradoException("la plantilla de rutina", idPlantilla);
        }

        if (request.IdObjetivo is not { } idObjetivo) return null;

        // Se prefiere la plantilla de la organización sobre la global, y la que más se
        // acerque a los días por semana que pidió el socio.
        return await _db.Rutinas
            .Where(r => r.Activa
                        && r.IdObjetivo == idObjetivo
                        && (r.IdOrganizacion == null || r.IdOrganizacion == idOrganizacion)
                        && r.Detalle.Any())
            .OrderByDescending(r => r.IdOrganizacion != null)
            .ThenBy(r => r.DiasPorSemana == request.DiasPorSemana ? 0 : 1)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>Copia el detalle de una plantilla al formato de propuesta.</summary>
    private async Task<IReadOnlyList<DiaDeRutinaDto>> CopiarDetalleDePlantillaAsync(
        int idPlantilla, CancellationToken ct)
    {
        var detalle = await _db.RutinaDetalles
            .Where(d => d.IdRutina == idPlantilla)
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

        return detalle
            .GroupBy(d => d.Dia)
            .OrderBy(g => g.Key)
            .Select(g => new DiaDeRutinaDto
            {
                Dia = g.Key,
                Ejercicios = g.Select(x => x.Ejercicio).ToList()
            })
            .ToList();
    }

    /// <summary>
    /// Arma una rutina desde cero repartiendo los grupos musculares entre los días, para
    /// que ningún día quede concentrado en un solo grupo.
    /// </summary>
    private async Task<IReadOnlyList<DiaDeRutinaDto>> ArmarRutinaAutomaticaAsync(
        GenerarRutinaRequest request, CancellationToken ct)
    {
        var ejercicios = await _db.Ejercicios
            .Select(e => new
            {
                e.Id,
                e.Nombre,
                e.IdGrupo,
                Grupo = e.Grupo.Nombre,
                e.VideoUrl
            })
            .ToListAsync(ct);

        if (ejercicios.Count == 0)
            throw new ReglaDeNegocioException(
                "No hay ejercicios cargados en el catálogo, así que no se puede generar una rutina.");

        // Se agrupa por grupo muscular y se recorre en round-robin: el día 1 empieza por
        // un grupo, el día 2 por el siguiente, y así se evita repetir siempre lo mismo.
        var porGrupo = ejercicios
            .GroupBy(e => e.Grupo)
            .OrderBy(g => g.Key)
            .Select(g => g.OrderBy(_ => Random.Shared.Next()).ToList())
            .ToList();

        var dias = new List<DiaDeRutinaDto>();
        var cursor = 0;

        for (var dia = 1; dia <= request.DiasPorSemana; dia++)
        {
            var delDia = new List<EjercicioDeRutinaDto>();
            var usados = new HashSet<int>();

            for (var i = 0; i < request.EjerciciosPorDia; i++)
            {
                var grupo = porGrupo[cursor++ % porGrupo.Count];
                var elegido = grupo.FirstOrDefault(e => !usados.Contains(e.Id));

                if (elegido is null) continue;

                usados.Add(elegido.Id);

                delDia.Add(new EjercicioDeRutinaDto
                {
                    Orden = delDia.Count + 1,
                    IdEjercicio = elegido.Id,
                    Ejercicio = elegido.Nombre,
                    Grupo = elegido.Grupo,
                    Series = 3,
                    Repeticiones = "10-12",
                    DescansoSeg = 90,
                    VideoUrl = elegido.VideoUrl
                });
            }

            dias.Add(new DiaDeRutinaDto { Dia = dia, Ejercicios = delDia });
        }

        return dias;
    }

    /// <summary>Trae la rutina activa del socio con su detalle completo.</summary>
    private async Task<RutinaDeSocioDto?> ObtenerRutinaVigenteAsync(int idSocio, CancellationToken ct)
    {
        var cabecera = await ProyectarCabecera(_db.SocioRutinas.Where(r => r.IdSocio == idSocio))
            .OrderByDescending(r => r.Activa)
            .ThenByDescending(r => r.FechaInicio)
            .ThenByDescending(r => r.Id)
            .FirstOrDefaultAsync(ct);

        if (cabecera is null) return null;

        var detalle = await _db.SocioRutinaDetalles
            .Where(d => d.IdSocioRutina == cabecera.Id)
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
                    Notas = d.Notas,
                    VideoUrl = d.Ejercicio.VideoUrl
                }
            })
            .ToListAsync(ct);

        return new RutinaDeSocioDto
        {
            Id = cabecera.Id,
            IdSocio = cabecera.IdSocio,
            Socio = cabecera.Socio,
            Nombre = cabecera.Nombre,
            IdPlantilla = cabecera.IdPlantilla,
            Plantilla = cabecera.Plantilla,
            IdEntrenador = cabecera.IdEntrenador,
            Entrenador = cabecera.Entrenador,
            FechaInicio = cabecera.FechaInicio,
            FechaFin = cabecera.FechaFin,
            Activa = cabecera.Activa,
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

    private static IQueryable<RutinaDeSocioDto> ProyectarCabecera(IQueryable<SocioRutina> consulta) =>
        consulta.Select(r => new RutinaDeSocioDto
        {
            Id = r.Id,
            IdSocio = r.IdSocio,
            Socio = (r.Socio.Usuario.Nombre + " " + r.Socio.Usuario.Apellido).Trim(),
            Nombre = r.Nombre,
            IdPlantilla = r.IdRutina,
            Plantilla = r.Plantilla != null ? r.Plantilla.Nombre : null,
            IdEntrenador = r.IdEntrenador,
            Entrenador = r.Entrenador != null
                ? (r.Entrenador.Nombre + " " + r.Entrenador.Apellido).Trim()
                : null,
            FechaInicio = r.FechaInicio,
            FechaFin = r.FechaFin,
            Activa = r.Activa
        });
}
