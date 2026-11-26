using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Extensions;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Features.Dietas.Dtos;
using VirtualGymAssistant.Application.Features.Socios;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Entities;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.Dietas;

/// <summary>
/// Casos de uso "Generar y guardar una dieta" y "Consultar dieta asignada".
///
/// Al igual que con las rutinas, el socio tiene una sola dieta activa: al guardar una
/// nueva, la anterior se archiva para conservar el historial.
/// </summary>
public class ServicioDeDietas
{
    private readonly IVirtualGymDbContext _db;
    private readonly IServicioDeContexto _contexto;
    private readonly IProveedorDeFechaHora _reloj;
    private readonly GestorDeFichasDeSocio _fichasDeSocio;

    public ServicioDeDietas(
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
    /// Arma una propuesta de dieta SIN guardarla, eligiendo un alimento por categoría y
    /// por día entre los que corresponden al objetivo pedido.
    /// </summary>
    public async Task<IReadOnlyList<DiaDeDietaDto>> GenerarPropuestaAsync(
        GenerarDietaRequest request, CancellationToken ct = default)
    {
        var consulta = _db.Alimentos.Where(a => a.IdCategoria != null);

        if (request.IdObjetivo is { } idObjetivo)
            consulta = consulta.Where(a => a.IdObjetivo == idObjetivo);

        if (request.IdsDeCategorias.Count > 0)
            consulta = consulta.Where(a => request.IdsDeCategorias.Contains(a.IdCategoria!.Value));

        var alimentos = await consulta
            .Select(a => new
            {
                a.Id,
                a.Nombre,
                Categoria = a.Categoria!.Nombre,
                a.Calorias,
                a.Proteinas,
                a.Carbohidratos,
                a.Grasas
            })
            .ToListAsync(ct);

        if (alimentos.Count == 0)
            throw new ReglaDeNegocioException(
                "No hay alimentos cargados para ese objetivo y esas categorías. " +
                "Probá con otro objetivo o pedile al nutricionista que amplíe el catálogo.");

        var porCategoria = alimentos
            .GroupBy(a => a.Categoria)
            .OrderBy(g => g.Key)
            .ToList();

        var dias = new List<DiaDeDietaDto>();

        for (var numero = 1; numero <= request.Dias; numero++)
        {
            var comidas = porCategoria
                .Select(grupo =>
                {
                    // Un alimento distinto por día dentro de cada categoría, para que el
                    // plan no repita siempre lo mismo.
                    var opciones = grupo.ToList();
                    var elegido = opciones[Random.Shared.Next(opciones.Count)];

                    return new ComidaDto
                    {
                        Categoria = grupo.Key,
                        Alimento = elegido.Nombre,
                        IdAlimento = elegido.Id,
                        Calorias = elegido.Calorias,
                        Proteinas = elegido.Proteinas,
                        Carbohidratos = elegido.Carbohidratos,
                        Grasas = elegido.Grasas
                    };
                })
                .ToList();

            dias.Add(new DiaDeDietaDto { Dia = $"Día {numero}", Comidas = comidas });
        }

        return dias;
    }

    /// <summary>Dieta vigente del socio autenticado en la sucursal activa.</summary>
    public async Task<DietaDeSocioDto?> ObtenerMiDietaAsync(CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        var idSocio = await _fichasDeSocio.BuscarIdAsync(contexto.IdUsuario, idGimnasio, ct);
        if (idSocio is null) return null;

        return await ObtenerDietaVigenteAsync(idSocio.Value, ct);
    }

    /// <summary>Dieta vigente de un socio puntual (uso del nutricionista).</summary>
    public async Task<DietaDeSocioDto?> ObtenerDeSocioAsync(int idSocio, CancellationToken ct = default)
    {
        await ExigirAccesoAlSocioAsync(idSocio, ct);

        return await ObtenerDietaVigenteAsync(idSocio, ct);
    }

    /// <summary>Historial de dietas de un socio.</summary>
    public async Task<IReadOnlyList<DietaDeSocioDto>> ListarHistorialDeSocioAsync(
        int idSocio, CancellationToken ct = default)
    {
        await ExigirAccesoAlSocioAsync(idSocio, ct);

        return await ProyectarCabecera(_db.SocioDietas.Where(d => d.IdSocio == idSocio))
            .OrderByDescending(d => d.Activa)
            .ThenByDescending(d => d.FechaInicio)
            .ToListAsync(ct);
    }

    /// <summary>El socio guarda la dieta que generó o armó a mano.</summary>
    public async Task<DietaDeSocioDto> GuardarMiDietaAsync(
        GuardarDietaRequest request, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        var idSocio = await _fichasDeSocio.ObtenerOCrearAsync(contexto.IdUsuario, idGimnasio, ct);

        return await GuardarAsync(idSocio, request, idNutricionista: null, ct);
    }

    /// <summary>El nutricionista arma y asigna una dieta a un socio de su sucursal.</summary>
    public async Task<DietaDeSocioDto> AsignarASocioAsync(
        AsignarDietaRequest request, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Nutricionista, RolesDelSistema.Administrador);

        var idGimnasio = contexto.GimnasioActivoRequerido;

        if (!await _db.Socios.AnyAsync(s => s.Id == request.IdSocio && s.IdGimnasio == idGimnasio, ct))
            throw new NoEncontradoException("el socio", request.IdSocio);

        return await GuardarAsync(request.IdSocio, request, contexto.IdUsuario, ct);
    }

    // ------------------------------------------------------------------

    private async Task<DietaDeSocioDto> GuardarAsync(
        int idSocio, GuardarDietaRequest request, int? idNutricionista, CancellationToken ct)
    {
        if (request.Dias.Count == 0)
            throw new ReglaDeNegocioException("La dieta tiene que tener al menos un día cargado.");

        if (request.IdObjetivo is { } idObjetivo &&
            !await _db.Objetivos.AnyAsync(o => o.Id == idObjetivo, ct))
            throw new NoEncontradoException("el objetivo", idObjetivo);

        var anteriores = await _db.SocioDietas
            .Where(d => d.IdSocio == idSocio && d.Activa)
            .ToListAsync(ct);

        foreach (var anterior in anteriores)
        {
            anterior.Activa = false;
            anterior.FechaFin = _reloj.Hoy;
        }

        var dieta = new SocioDieta
        {
            IdSocio = idSocio,
            IdNutricionista = idNutricionista,
            Nombre = request.Nombre.NormalizarONull() ?? "Dieta personalizada",
            IdObjetivo = request.IdObjetivo,
            FechaInicio = _reloj.Hoy,
            Activa = true
        };

        _db.SocioDietas.Add(dieta);
        await _db.SaveChangesAsync(ct);

        foreach (var dia in request.Dias)
        {
            foreach (var comida in dia.Comidas)
            {
                _db.SocioDietaDetalles.Add(new SocioDietaDetalle
                {
                    IdSocioDieta = dieta.Id,
                    Dia = dia.Dia.Trim(),
                    Categoria = comida.Categoria.Trim(),
                    Alimento = comida.Alimento.Trim(),
                    IdAlimento = comida.IdAlimento
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        return (await ObtenerDietaVigenteAsync(idSocio, ct))!;
    }

    /// <summary>
    /// Un socio ve su propia dieta; para ver la de otro hay que ser nutricionista o
    /// administración de esa sucursal.
    /// </summary>
    private async Task ExigirAccesoAlSocioAsync(int idSocio, CancellationToken ct)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        var socio = await _db.Socios
            .FirstOrDefaultAsync(s => s.Id == idSocio && s.IdGimnasio == idGimnasio, ct)
            ?? throw new NoEncontradoException("el socio", idSocio);

        if (socio.IdUsuario == contexto.IdUsuario) return;

        contexto.ExigirRol(RolesDelSistema.Nutricionista, RolesDelSistema.Administrador);
    }

    private async Task<DietaDeSocioDto?> ObtenerDietaVigenteAsync(int idSocio, CancellationToken ct)
    {
        var cabecera = await ProyectarCabecera(_db.SocioDietas.Where(d => d.IdSocio == idSocio))
            .OrderByDescending(d => d.Activa)
            .ThenByDescending(d => d.FechaInicio)
            .ThenByDescending(d => d.Id)
            .FirstOrDefaultAsync(ct);

        if (cabecera is null) return null;

        var detalle = await _db.SocioDietaDetalles
            .Where(d => d.IdSocioDieta == cabecera.Id)
            .OrderBy(d => d.Id)
            .Select(d => new
            {
                d.Dia,
                Comida = new ComidaDto
                {
                    Categoria = d.Categoria,
                    Alimento = d.Alimento,
                    IdAlimento = d.IdAlimento,
                    Calorias = d.AlimentoDelCatalogo != null ? d.AlimentoDelCatalogo.Calorias : null,
                    Proteinas = d.AlimentoDelCatalogo != null ? d.AlimentoDelCatalogo.Proteinas : null,
                    Carbohidratos = d.AlimentoDelCatalogo != null ? d.AlimentoDelCatalogo.Carbohidratos : null,
                    Grasas = d.AlimentoDelCatalogo != null ? d.AlimentoDelCatalogo.Grasas : null
                }
            })
            .ToListAsync(ct);

        return new DietaDeSocioDto
        {
            Id = cabecera.Id,
            IdSocio = cabecera.IdSocio,
            Socio = cabecera.Socio,
            Nombre = cabecera.Nombre,
            IdObjetivo = cabecera.IdObjetivo,
            Objetivo = cabecera.Objetivo,
            IdNutricionista = cabecera.IdNutricionista,
            Nutricionista = cabecera.Nutricionista,
            FechaInicio = cabecera.FechaInicio,
            FechaFin = cabecera.FechaFin,
            Activa = cabecera.Activa,
            Dias = detalle
                .GroupBy(d => d.Dia)
                .Select(g => new DiaDeDietaDto
                {
                    Dia = g.Key,
                    Comidas = g.Select(x => x.Comida).ToList()
                })
                .ToList()
        };
    }

    private static IQueryable<DietaDeSocioDto> ProyectarCabecera(IQueryable<SocioDieta> consulta) =>
        consulta.Select(d => new DietaDeSocioDto
        {
            Id = d.Id,
            IdSocio = d.IdSocio,
            Socio = (d.Socio.Usuario.Nombre + " " + d.Socio.Usuario.Apellido).Trim(),
            Nombre = d.Nombre,
            IdObjetivo = d.IdObjetivo,
            Objetivo = d.Objetivo != null ? d.Objetivo.Nombre : null,
            IdNutricionista = d.IdNutricionista,
            Nutricionista = d.Nutricionista != null
                ? (d.Nutricionista.Nombre + " " + d.Nutricionista.Apellido).Trim()
                : null,
            FechaInicio = d.FechaInicio,
            FechaFin = d.FechaFin,
            Activa = d.Activa
        });
}
