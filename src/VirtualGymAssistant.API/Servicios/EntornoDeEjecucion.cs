using VirtualGymAssistant.Application.Common.Interfaces;

namespace VirtualGymAssistant.API.Servicios;

/// <summary>
/// Expone el entorno de ASP.NET Core a la capa Application sin acoplarla al host.
/// </summary>
public class EntornoDeEjecucion : IEntornoDeEjecucion
{
    private readonly IWebHostEnvironment _entorno;

    public EntornoDeEjecucion(IWebHostEnvironment entorno) => _entorno = entorno;

    public bool EsDesarrollo => _entorno.IsDevelopment();
}
