namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Habilita a un socio a entrenar en una sucursal distinta a la de origen
/// (se usa cuando su plan tiene acceso multi-sede).
/// Tabla: dbo.SocioAccesoGimnasio (clave compuesta)
/// </summary>
public class SocioAccesoGimnasio
{
    public int IdSocio { get; set; }
    public int IdGimnasio { get; set; }
    public DateOnly FechaDesde { get; set; }

    public Socio Socio { get; set; } = null!;
    public Gimnasio Gimnasio { get; set; } = null!;
}
