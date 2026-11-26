namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Inventario: cuántas unidades de una máquina tiene una sucursal y en qué estado.
/// Tabla: dbo.GimnasioMaquinas
/// </summary>
public class GimnasioMaquina
{
    public int Id { get; set; }
    public int IdGimnasio { get; set; }
    public int IdMaquina { get; set; }
    public int Cantidad { get; set; } = 1;

    /// <summary>Estado operativo: Operativa, En reparación, Fuera de servicio.</summary>
    public string Estado { get; set; } = "Operativa";

    public Gimnasio Gimnasio { get; set; } = null!;
    public Maquina Maquina { get; set; } = null!;
}
