using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Constants;
using VirtualGymAssistant.Application.Common.Extensions;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Common.Models;
using VirtualGymAssistant.Application.Features.Membresias;
using VirtualGymAssistant.Application.Features.Pagos.Dtos;
using VirtualGymAssistant.Application.Features.Socios;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Entities;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.Pagos;

/// <summary>
/// Casos de uso del módulo de pagos: el socio carga su cuota, el mostrador la aprueba
/// o la rechaza, y la administración consulta la grilla anual y la recaudación.
///
/// Reglas centrales del módulo:
///   - Un único pago por socio y período (año + mes). Lo garantiza también la base.
///   - Un pago rechazado o vencido se reintenta reutilizando el mismo registro.
///   - Al aprobarse un pago, el socio queda con una membresía activa de ese plan.
/// </summary>
public class ServicioDePagos
{
    /// <summary>Cuántos meses hacia adelante se admite pagar por anticipado.</summary>
    private const int MesesDeAnticipoPermitidos = 12;

    /// <summary>
    /// Transiciones válidas del estado de un pago. Todo lo que no esté acá se rechaza,
    /// así no se puede, por ejemplo, "aprobar" un pago ya anulado.
    /// </summary>
    private static readonly Dictionary<string, string[]> TransicionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        [EstadosDePago.Pendiente] = [EstadosDePago.Pagado, EstadosDePago.Rechazado, EstadosDePago.Vencido, EstadosDePago.Anulado],
        [EstadosDePago.Rechazado] = [EstadosDePago.Pendiente, EstadosDePago.Anulado],
        [EstadosDePago.Vencido] = [EstadosDePago.Pendiente, EstadosDePago.Pagado, EstadosDePago.Anulado],
        [EstadosDePago.Pagado] = [EstadosDePago.Anulado],
        [EstadosDePago.Anulado] = [],
    };

    private readonly IVirtualGymDbContext _db;
    private readonly IServicioDeContexto _contexto;
    private readonly IProveedorDeFechaHora _reloj;
    private readonly GestorDeFichasDeSocio _fichasDeSocio;
    private readonly ServicioDeMembresias _membresias;
    private readonly IServicioDeNotificaciones _notificaciones;

    public ServicioDePagos(
        IVirtualGymDbContext db,
        IServicioDeContexto contexto,
        IProveedorDeFechaHora reloj,
        GestorDeFichasDeSocio fichasDeSocio,
        ServicioDeMembresias membresias,
        IServicioDeNotificaciones notificaciones)
    {
        _db = db;
        _contexto = contexto;
        _reloj = reloj;
        _fichasDeSocio = fichasDeSocio;
        _membresias = membresias;
        _notificaciones = notificaciones;
    }

    /// <summary>
    /// Caso de uso "Pagar la cuota mensual" y "Reintentar un pago rechazado".
    /// El pago queda en estado Pendiente hasta que el mostrador lo confirme.
    /// </summary>
    public async Task<PagoDto> RegistrarAsync(RegistrarPagoRequest request, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        // Regla automática del sistema: si todavía no es socio de esta sede, se le crea la ficha.
        var idSocio = await _fichasDeSocio.ObtenerOCrearAsync(contexto.IdUsuario, idGimnasio, ct);

        var (anio, mes) = ResolverPeriodo(request.Anio, request.Mes);
        var plan = await _membresias.ObtenerPlanDeLaOrganizacionAsync(
            request.IdPlan, contexto.OrganizacionActivaRequerida, ct);

        await ExigirFormaDePagoVigenteAsync(request.IdFormaPago, ct);

        var pago = await RegistrarOReintentarAsync(
            idSocio, idGimnasio, anio, mes, plan, request.IdFormaPago,
            request.Comprobante, plan.Precio, EstadosDePago.Pendiente, ct);

        await _db.SaveChangesAsync(ct);

        return await ObtenerDtoAsync(pago.Id, ct);
    }

    /// <summary>
    /// Cobro tomado en el mostrador. A diferencia del pago que carga el socio, este
    /// nace aprobado: lo está registrando el propio personal que recibió el dinero.
    /// </summary>
    public async Task<PagoDto> RegistrarCobroPresencialAsync(
        RegistrarCobroPresencialRequest request, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Recepcionista, RolesDelSistema.Administrador);

        var idGimnasio = contexto.GimnasioActivoRequerido;

        var socio = await _db.Socios
            .FirstOrDefaultAsync(s => s.Id == request.IdSocio && s.IdGimnasio == idGimnasio, ct)
            ?? throw new NoEncontradoException("el socio", request.IdSocio);

        var (anio, mes) = ResolverPeriodo(request.Anio, request.Mes);
        var plan = await _membresias.ObtenerPlanDeLaOrganizacionAsync(
            request.IdPlan, contexto.OrganizacionActivaRequerida, ct);

        await ExigirFormaDePagoVigenteAsync(request.IdFormaPago, ct);

        var pago = await RegistrarOReintentarAsync(
            socio.Id, idGimnasio, anio, mes, plan, request.IdFormaPago,
            request.Comprobante, request.Monto ?? plan.Precio, EstadosDePago.Pagado, ct);

        pago.FechaPago = _reloj.Ahora;

        // Un cobro confirmado deja al socio con la membresía correspondiente al plan.
        var membresia = await _membresias.AsegurarMembresiaActivaAsync(
            socio.Id, idGimnasio, plan, request.Monto, ct);

        await _db.SaveChangesAsync(ct);

        pago.IdMembresia = membresia.Id;
        await _db.SaveChangesAsync(ct);

        return await ObtenerDtoAsync(pago.Id, ct);
    }

    /// <summary>
    /// Caso de uso "Consultar el estado de sus pagos": los 12 meses del año pedido para
    /// el socio autenticado en la sucursal activa.
    /// </summary>
    public async Task<IReadOnlyList<PagoDto>> ListarMisPagosAsync(
        int? anio = null, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        var idSocio = await _fichasDeSocio.BuscarIdAsync(contexto.IdUsuario, idGimnasio, ct);
        if (idSocio is null) return [];

        var anioConsultado = anio ?? _reloj.Hoy.Year;

        return await Proyectar(_db.Pagos
                .Where(p => p.IdSocio == idSocio && p.PeriodoAnio == anioConsultado))
            .OrderBy(p => p.PeriodoMes)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Caso de uso "Aprobar o rechazar pagos pendientes": bandeja de entrada del mostrador.
    /// </summary>
    public async Task<ResultadoPaginado<PagoDto>> ListarPendientesAsync(
        ParametrosDePaginacion paginacion, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Recepcionista, RolesDelSistema.Administrador);

        var idGimnasio = contexto.GimnasioActivoRequerido;

        var consulta = Proyectar(_db.Pagos
                .Where(p => p.IdGimnasio == idGimnasio && p.Estado.Nombre == EstadosDePago.Pendiente))
            .OrderBy(p => p.PeriodoAnio)
            .ThenBy(p => p.PeriodoMes)
            .ThenBy(p => p.Socio);

        return await consulta.PaginarAsync(paginacion, ct);
    }

    /// <summary>Listado general de pagos de la sucursal con filtros de período y estado.</summary>
    public async Task<ResultadoPaginado<PagoDto>> ListarAsync(
        int? anio,
        int? mes,
        int? idEstado,
        int? idSocio,
        ParametrosDePaginacion paginacion,
        CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Recepcionista, RolesDelSistema.Administrador);

        var idGimnasio = contexto.GimnasioActivoRequerido;
        var consulta = _db.Pagos.Where(p => p.IdGimnasio == idGimnasio);

        if (anio is { } a) consulta = consulta.Where(p => p.PeriodoAnio == a);
        if (mes is { } m) consulta = consulta.Where(p => p.PeriodoMes == m);
        if (idEstado is { } e) consulta = consulta.Where(p => p.IdEstadoPago == e);
        if (idSocio is { } s) consulta = consulta.Where(p => p.IdSocio == s);

        var proyeccion = Proyectar(consulta)
            .OrderByDescending(p => p.PeriodoAnio)
            .ThenByDescending(p => p.PeriodoMes)
            .ThenBy(p => p.Socio);

        return await proyeccion.PaginarAsync(paginacion, ct);
    }

    /// <summary>
    /// Aprueba, rechaza, vence o anula un pago.
    /// Al aprobarlo se sella la fecha de pago y se asegura la membresía del socio.
    /// </summary>
    public async Task<PagoDto> CambiarEstadoAsync(
        int idPago, CambiarEstadoDePagoRequest request, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Recepcionista, RolesDelSistema.Administrador);

        var idGimnasio = contexto.GimnasioActivoRequerido;

        var pago = await _db.Pagos
            .Include(p => p.Estado)
            .FirstOrDefaultAsync(p => p.Id == idPago && p.IdGimnasio == idGimnasio, ct)
            ?? throw new NoEncontradoException("el pago", idPago);

        var estadoDestino = await _db.PagoEstados
            .FirstOrDefaultAsync(e => e.Nombre == request.Estado, ct)
            ?? throw new ReglaDeNegocioException(
                $"El estado \"{request.Estado}\" no existe. Valores válidos: " +
                $"{EstadosDePago.Pendiente}, {EstadosDePago.Pagado}, {EstadosDePago.Rechazado}, " +
                $"{EstadosDePago.Vencido}, {EstadosDePago.Anulado}.");

        ExigirTransicionValida(pago.Estado.Nombre, estadoDestino.Nombre);

        pago.IdEstadoPago = estadoDestino.Id;
        pago.Observaciones = request.Observaciones.NormalizarONull() ?? pago.Observaciones;

        if (estadoDestino.Nombre == EstadosDePago.Pagado)
        {
            pago.FechaPago = _reloj.Ahora;

            // Un pago aprobado implica que el socio tiene ese plan vigente.
            if (pago.IdPlan is { } idPlan)
            {
                var plan = await _db.Planes.FirstOrDefaultAsync(p => p.Id == idPlan, ct);

                if (plan is not null)
                {
                    var membresia = await _membresias.AsegurarMembresiaActivaAsync(
                        pago.IdSocio, idGimnasio, plan, pago.Monto, ct);

                    await _db.SaveChangesAsync(ct);
                    pago.IdMembresia = membresia.Id;
                }
            }
        }
        else
        {
            // Si se revierte la aprobación, la fecha de pago deja de tener sentido.
            pago.FechaPago = null;
        }

        await _db.SaveChangesAsync(ct);

        return await ObtenerDtoAsync(idPago, ct);
    }

    /// <summary>
    /// Manda por mail la constancia de un pago ya aprobado. El PDF lo arma el navegador
    /// (mismo criterio que la rutina o el plan de alimentación, que también se exportan
    /// del lado del cliente); acá sólo se valida el pago y se lo adjunta al mail.
    /// </summary>
    public async Task EnviarConstanciaAsync(
        int idPago, EnviarConstanciaDePagoRequest request, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Recepcionista, RolesDelSistema.Administrador);

        var idGimnasio = contexto.GimnasioActivoRequerido;

        var pago = await _db.Pagos
            .Include(p => p.Estado)
            .Include(p => p.Socio).ThenInclude(s => s.Usuario)
            .FirstOrDefaultAsync(p => p.Id == idPago && p.IdGimnasio == idGimnasio, ct)
            ?? throw new NoEncontradoException("el pago", idPago);

        if (pago.Estado.Nombre != EstadosDePago.Pagado)
            throw new ReglaDeNegocioException("Sólo se puede enviar la constancia de un pago aprobado.");

        byte[] pdf;

        try
        {
            pdf = Convert.FromBase64String(request.PdfBase64);
        }
        catch (FormatException)
        {
            throw new ValidacionException(nameof(request.PdfBase64), "El PDF no llegó en un formato válido.");
        }

        const int maximoBytes = 5 * 1024 * 1024;

        if (pdf.Length is 0 or > maximoBytes)
            throw new ReglaDeNegocioException("El PDF de la constancia no tiene un tamaño válido.");

        var nombreSocio = $"{pago.Socio.Usuario.Nombre} {pago.Socio.Usuario.Apellido}".Trim();

        await _notificaciones.EnviarConstanciaDePagoAsync(pago.Socio.Usuario.Mail, nombreSocio, pdf, ct);
    }

    /// <summary>
    /// Caso de uso "Ver la administración de pagos": grilla anual con el estado de las
    /// 12 cuotas de cada socio de la sucursal.
    /// </summary>
    public async Task<IReadOnlyList<FilaDeGrillaAnualDto>> ObtenerGrillaAnualAsync(
        int? anio = null, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Recepcionista, RolesDelSistema.Administrador);

        var idGimnasio = contexto.GimnasioActivoRequerido;
        var anioConsultado = anio ?? _reloj.Hoy.Year;

        // Se traen socios y pagos por separado y se arma la grilla en memoria: es mucho
        // más legible que una consulta con doce columnas condicionales, y el volumen por
        // sucursal y por año lo permite sin problemas.
        var socios = await _db.Socios
            .Where(s => s.IdGimnasio == idGimnasio)
            .OrderBy(s => s.Usuario.Apellido)
            .ThenBy(s => s.Usuario.Nombre)
            .Select(s => new
            {
                s.Id,
                s.NumeroSocio,
                s.Usuario.Username,
                NombreCompleto = (s.Usuario.Nombre + " " + s.Usuario.Apellido).Trim(),
                Plan = s.Membresias.Where(m => m.Activa).Select(m => m.Plan.Nombre).FirstOrDefault()
            })
            .ToListAsync(ct);

        var pagos = await _db.Pagos
            .Where(p => p.IdGimnasio == idGimnasio && p.PeriodoAnio == anioConsultado)
            .Select(p => new { p.IdSocio, p.PeriodoMes, Estado = p.Estado.Nombre, p.Monto })
            .ToListAsync(ct);

        var pagosPorSocio = pagos.ToLookup(p => p.IdSocio);

        return socios.Select(socio =>
        {
            var delSocio = pagosPorSocio[socio.Id].ToList();
            var estados = new string?[12];

            foreach (var pago in delSocio)
                estados[pago.PeriodoMes - 1] = pago.Estado;

            return new FilaDeGrillaAnualDto
            {
                IdSocio = socio.Id,
                Socio = socio.NombreCompleto,
                Username = socio.Username,
                NumeroSocio = socio.NumeroSocio,
                Plan = socio.Plan ?? "Sin plan",
                EstadoPorMes = estados,
                MesesPagados = delSocio.Count(p => p.Estado == EstadosDePago.Pagado),
                TotalPagado = delSocio.Where(p => p.Estado == EstadosDePago.Pagado).Sum(p => p.Monto),
                TotalPendiente = delSocio.Where(p => p.Estado == EstadosDePago.Pendiente).Sum(p => p.Monto)
            };
        }).ToList();
    }

    /// <summary>
    /// Caso de uso "Consultar métricas de la sucursal": socios, cuotas y recaudación.
    /// </summary>
    public async Task<ResumenDeCobranzaDto> ObtenerResumenAsync(
        int? anio = null, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Recepcionista, RolesDelSistema.Administrador);

        var idGimnasio = contexto.GimnasioActivoRequerido;
        var anioConsultado = anio ?? _reloj.Hoy.Year;
        var mesActual = _reloj.Hoy.Month;

        var sociosTotales = await _db.Socios.CountAsync(s => s.IdGimnasio == idGimnasio, ct);

        var sociosActivos = await _db.Socios
            .CountAsync(s => s.IdGimnasio == idGimnasio && s.Estado.Nombre == EstadosDeSocio.Activo, ct);

        var delAnio = await _db.Pagos
            .Where(p => p.IdGimnasio == idGimnasio && p.PeriodoAnio == anioConsultado)
            .Select(p => new { p.PeriodoMes, Estado = p.Estado.Nombre, p.Monto, p.IdSocio })
            .ToListAsync(ct);

        var pagados = delAnio.Where(p => p.Estado == EstadosDePago.Pagado).ToList();
        var pendientes = delAnio.Where(p => p.Estado == EstadosDePago.Pendiente).ToList();

        var sociosAlDia = pagados
            .Where(p => p.PeriodoMes == mesActual)
            .Select(p => p.IdSocio)
            .Distinct()
            .Count();

        return new ResumenDeCobranzaDto
        {
            Anio = anioConsultado,
            Mes = mesActual,
            SociosTotales = sociosTotales,
            SociosActivos = sociosActivos,
            CuotasPagadas = pagados.Count,
            CuotasPendientes = pendientes.Count,
            CuotasRechazadas = delAnio.Count(p => p.Estado == EstadosDePago.Rechazado),
            RecaudacionAnual = pagados.Sum(p => p.Monto),
            RecaudacionDelMes = pagados.Where(p => p.PeriodoMes == mesActual).Sum(p => p.Monto),
            MontoPendienteDeAprobacion = pendientes.Sum(p => p.Monto),
            SociosAlDia = sociosAlDia,
            SociosConDeuda = Math.Max(0, sociosActivos - sociosAlDia)
        };
    }

    // ------------------------------------------------------------------
    // Reglas internas
    // ------------------------------------------------------------------

    /// <summary>
    /// Crea el pago del período o, si el período ya tiene un pago rechazado / vencido /
    /// anulado, reutiliza ese registro. No se puede crear uno nuevo porque la base impone
    /// un único pago por socio y período.
    /// </summary>
    private async Task<Pago> RegistrarOReintentarAsync(
        int idSocio,
        int idGimnasio,
        int anio,
        int mes,
        Plan plan,
        int idFormaPago,
        string? comprobante,
        decimal monto,
        string estadoInicial,
        CancellationToken ct)
    {
        var idEstado = await _db.PagoEstados
            .Where(e => e.Nombre == estadoInicial)
            .Select(e => e.Id)
            .FirstAsync(ct);

        var existente = await _db.Pagos
            .Include(p => p.Estado)
            .FirstOrDefaultAsync(p => p.IdSocio == idSocio && p.PeriodoAnio == anio && p.PeriodoMes == mes, ct);

        if (existente is not null)
        {
            if (EstadosDePago.BloqueanNuevoPago.Contains(existente.Estado.Nombre))
                throw new ReglaDeNegocioException(
                    $"{Meses.Nombre(mes)} de {anio} ya tiene un pago en estado " +
                    $"{existente.Estado.Nombre}. No se puede volver a pagar ese período.");

            // Reintento: se reutiliza el registro y vuelve a quedar pendiente.
            existente.IdFormaPago = idFormaPago;
            existente.IdPlan = plan.Id;
            existente.Monto = monto;
            existente.IdEstadoPago = idEstado;
            existente.FechaPago = null;
            existente.FechaRegistro = _reloj.Ahora;
            existente.FechaVencimiento = UltimoDiaDelMes(anio, mes);
            existente.Comprobante = comprobante.NormalizarONull();
            existente.Observaciones = null;

            return existente;
        }

        var nuevo = new Pago
        {
            IdSocio = idSocio,
            IdGimnasio = idGimnasio,
            IdPlan = plan.Id,
            IdFormaPago = idFormaPago,
            PeriodoAnio = (short)anio,
            PeriodoMes = (byte)mes,
            Monto = monto,
            IdEstadoPago = idEstado,
            FechaVencimiento = UltimoDiaDelMes(anio, mes),
            FechaRegistro = _reloj.Ahora,
            Comprobante = comprobante.NormalizarONull(),

            // Si el socio ya tiene una membresía vigente, el pago queda vinculado a ella.
            IdMembresia = await _db.Membresias
                .Where(m => m.IdSocio == idSocio && m.Activa)
                .Select(m => (int?)m.Id)
                .FirstOrDefaultAsync(ct)
        };

        _db.Pagos.Add(nuevo);

        return nuevo;
    }

    /// <summary>
    /// Completa el período con el mes y el año en curso cuando no se indican, y verifica
    /// que no se intente pagar demasiado lejos en el futuro.
    /// </summary>
    private (int Anio, int Mes) ResolverPeriodo(int? anio, int? mes)
    {
        var hoy = _reloj.Hoy;
        var anioFinal = anio ?? hoy.Year;
        var mesFinal = mes ?? hoy.Month;

        if (mesFinal is < 1 or > 12)
            throw new ValidacionException(nameof(mes), "El mes debe estar entre 1 y 12.");

        var periodo = new DateOnly(anioFinal, mesFinal, 1);
        var limite = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(MesesDeAnticipoPermitidos);

        if (periodo > limite)
            throw new ReglaDeNegocioException(
                $"No se puede pagar con más de {MesesDeAnticipoPermitidos} meses de anticipación.");

        return (anioFinal, mesFinal);
    }

    /// <summary>Verifica que la forma de pago exista y siga vigente.</summary>
    private async Task ExigirFormaDePagoVigenteAsync(int idFormaPago, CancellationToken ct)
    {
        var vigente = await _db.FormasPago.AnyAsync(f => f.Id == idFormaPago && f.Activo, ct);

        if (!vigente)
            throw new NoEncontradoException("La forma de pago no existe o ya no está disponible.");
    }

    /// <summary>Corta la operación si el cambio de estado no está contemplado.</summary>
    private static void ExigirTransicionValida(string estadoActual, string estadoDestino)
    {
        if (string.Equals(estadoActual, estadoDestino, StringComparison.OrdinalIgnoreCase))
            throw new ReglaDeNegocioException($"El pago ya está en estado {estadoActual}.");

        var permitidos = TransicionesPermitidas.GetValueOrDefault(estadoActual, []);

        if (!permitidos.Contains(estadoDestino, StringComparer.OrdinalIgnoreCase))
            throw new ReglaDeNegocioException(
                $"No se puede pasar un pago de {estadoActual} a {estadoDestino}." +
                (permitidos.Length == 0
                    ? " Es un estado final."
                    : $" Desde {estadoActual} sólo se puede pasar a: {string.Join(", ", permitidos)}."));
    }

    /// <summary>La cuota de un período vence el último día de ese mes.</summary>
    private static DateOnly UltimoDiaDelMes(int anio, int mes) =>
        new(anio, mes, DateTime.DaysInMonth(anio, mes));

    private async Task<PagoDto> ObtenerDtoAsync(int idPago, CancellationToken ct) =>
        await Proyectar(_db.Pagos.Where(p => p.Id == idPago)).FirstAsync(ct);

    private static IQueryable<PagoDto> Proyectar(IQueryable<Pago> consulta) =>
        consulta.Select(p => new PagoDto
        {
            Id = p.Id,
            IdSocio = p.IdSocio,
            Socio = (p.Socio.Usuario.Nombre + " " + p.Socio.Usuario.Apellido).Trim(),
            Username = p.Socio.Usuario.Username,
            NumeroSocio = p.Socio.NumeroSocio,
            IdGimnasio = p.IdGimnasio,
            Sucursal = p.Gimnasio.Nombre,
            PeriodoAnio = p.PeriodoAnio,
            PeriodoMes = p.PeriodoMes,
            Mes = Meses.Nombres[p.PeriodoMes - 1],
            Monto = p.Monto,
            IdEstado = p.IdEstadoPago,
            Estado = p.Estado.Nombre,
            IdFormaPago = p.IdFormaPago,
            FormaPago = p.FormaPago.Nombre,
            IdPlan = p.IdPlan,
            Plan = p.Plan != null ? p.Plan.Nombre : null,
            FechaVencimiento = p.FechaVencimiento,
            FechaPago = p.FechaPago,
            FechaRegistro = p.FechaRegistro,
            Comprobante = p.Comprobante,
            Observaciones = p.Observaciones
        });
}
