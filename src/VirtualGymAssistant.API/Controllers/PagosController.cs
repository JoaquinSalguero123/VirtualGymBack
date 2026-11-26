using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Common.Models;
using VirtualGymAssistant.Application.Features.Pagos;
using VirtualGymAssistant.Application.Features.Pagos.Dtos;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Cuotas de los socios: carga, aprobación y reportes de cobranza.
/// </summary>
[Authorize]
[Tags("7 - Pagos")]
public class PagosController : ControladorBase
{
    private readonly ServicioDePagos _servicio;

    public PagosController(ServicioDePagos servicio) => _servicio = servicio;

    /// <summary>
    /// El socio carga el pago de su cuota mensual.
    /// </summary>
    /// <remarks>
    /// El pago queda en estado Pendiente hasta que el mostrador lo apruebe.
    /// Si el período ya tenía un pago rechazado o vencido, se reutiliza ese registro
    /// (reintento) en lugar de crear uno nuevo.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(PagoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagoDto>> Registrar(
        [FromBody] RegistrarPagoRequest request, CancellationToken ct)
    {
        var pago = await _servicio.RegistrarAsync(request, ct);

        return StatusCode(StatusCodes.Status201Created, pago);
    }

    /// <summary>
    /// Registra un cobro tomado en el mostrador. A diferencia del pago que carga el socio,
    /// este nace ya aprobado.
    /// </summary>
    [HttpPost("cobro-presencial")]
    [ProducesResponseType(typeof(PagoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PagoDto>> RegistrarCobroPresencial(
        [FromBody] RegistrarCobroPresencialRequest request, CancellationToken ct)
    {
        var pago = await _servicio.RegistrarCobroPresencialAsync(request, ct);

        return StatusCode(StatusCodes.Status201Created, pago);
    }

    /// <summary>
    /// Devuelve los pagos del socio autenticado para el año indicado.
    /// </summary>
    [HttpGet("mios")]
    [ProducesResponseType(typeof(IReadOnlyList<PagoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PagoDto>>> ListarMios(
        [FromQuery] int? anio, CancellationToken ct) =>
        Ok(await _servicio.ListarMisPagosAsync(anio, ct));

    /// <summary>
    /// Bandeja de pagos pendientes de aprobación de la sucursal activa.
    /// </summary>
    [HttpGet("pendientes")]
    [ProducesResponseType(typeof(ResultadoPaginado<PagoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginado<PagoDto>>> ListarPendientes(
        [FromQuery] int? pagina, [FromQuery] int? tamanioPagina, CancellationToken ct) =>
        Ok(await _servicio.ListarPendientesAsync(LeerPaginacion(pagina, tamanioPagina), ct));

    /// <summary>
    /// Lista los pagos de la sucursal con filtros por período, estado y socio.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<PagoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginado<PagoDto>>> Listar(
        [FromQuery] int? anio,
        [FromQuery] int? mes,
        [FromQuery] int? idEstado,
        [FromQuery] int? idSocio,
        [FromQuery] int? pagina,
        [FromQuery] int? tamanioPagina,
        CancellationToken ct) =>
        Ok(await _servicio.ListarAsync(anio, mes, idEstado, idSocio,
            LeerPaginacion(pagina, tamanioPagina), ct));

    /// <summary>
    /// Aprueba, rechaza, vence o anula un pago.
    /// </summary>
    /// <remarks>
    /// Sólo se permiten las transiciones que tienen sentido: por ejemplo, un pago anulado
    /// es un estado final y no se puede volver a aprobar. Al aprobarse, el socio queda con
    /// una membresía activa del plan correspondiente.
    /// </remarks>
    [HttpPut("{id:int}/estado")]
    [ProducesResponseType(typeof(PagoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagoDto>> CambiarEstado(
        int id, [FromBody] CambiarEstadoDePagoRequest request, CancellationToken ct) =>
        Ok(await _servicio.CambiarEstadoAsync(id, request, ct));

    /// <summary>
    /// Manda por mail la constancia de un pago ya aprobado, con el PDF adjunto.
    /// </summary>
    /// <remarks>
    /// El PDF se genera en el navegador (mismo criterio que la rutina o el plan de
    /// alimentación) y se manda acá codificado en base64 para adjuntarlo al mail.
    /// </remarks>
    [HttpPost("{id:int}/enviar-constancia")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EnviarConstancia(
        int id, [FromBody] EnviarConstanciaDePagoRequest request, CancellationToken ct)
    {
        await _servicio.EnviarConstanciaAsync(id, request, ct);

        return NoContent();
    }

    /// <summary>
    /// Grilla anual de cuotas: un renglón por socio con el estado de sus 12 meses.
    /// </summary>
    [HttpGet("grilla-anual")]
    [ProducesResponseType(typeof(IReadOnlyList<FilaDeGrillaAnualDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FilaDeGrillaAnualDto>>> ObtenerGrillaAnual(
        [FromQuery] int? anio, CancellationToken ct) =>
        Ok(await _servicio.ObtenerGrillaAnualAsync(anio, ct));

    /// <summary>
    /// Métricas de cobranza de la sucursal: socios, cuotas y recaudación.
    /// </summary>
    [HttpGet("resumen")]
    [ProducesResponseType(typeof(ResumenDeCobranzaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResumenDeCobranzaDto>> ObtenerResumen(
        [FromQuery] int? anio, CancellationToken ct) =>
        Ok(await _servicio.ObtenerResumenAsync(anio, ct));
}
