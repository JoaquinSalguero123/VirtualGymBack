namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Máquina del catálogo global. El stock por sucursal se lleva en <see cref="GimnasioMaquina"/>.
/// Tabla: dbo.Maquinas
/// </summary>
public class Maquina
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Foto { get; set; }

    public ICollection<GimnasioMaquina> EnSucursales { get; set; } = new List<GimnasioMaquina>();
}
