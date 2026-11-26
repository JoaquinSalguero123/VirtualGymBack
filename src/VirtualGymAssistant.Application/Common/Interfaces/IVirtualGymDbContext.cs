using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Domain.Entities;

namespace VirtualGymAssistant.Application.Common.Interfaces;

/// <summary>
/// Puerto de persistencia de la aplicación.
///
/// La capa Application trabaja contra esta interfaz y nunca contra el DbContext concreto:
/// así el negocio no queda atado a Entity Framework ni al motor de base de datos, y se
/// puede reemplazar la implementación (por ejemplo, por una en memoria) para los tests.
/// </summary>
public interface IVirtualGymDbContext
{
    // --- Identidad y organización ---
    DbSet<Usuario> Usuarios { get; }
    DbSet<Rol> Roles { get; }
    DbSet<UsuarioGimnasioRol> UsuarioGimnasioRoles { get; }
    DbSet<Organizacion> Organizaciones { get; }
    DbSet<Gimnasio> Gimnasios { get; }
    DbSet<Zona> Zonas { get; }

    // --- Socios ---
    DbSet<Socio> Socios { get; }
    DbSet<SocioEstado> SocioEstados { get; }
    DbSet<SocioAccesoGimnasio> SocioAccesosGimnasio { get; }

    // --- Comercial ---
    DbSet<Plan> Planes { get; }
    DbSet<Membresia> Membresias { get; }
    DbSet<Pago> Pagos { get; }
    DbSet<PagoEstado> PagoEstados { get; }
    DbSet<FormaPago> FormasPago { get; }

    // --- Operación diaria ---
    DbSet<Asistencia> Asistencias { get; }
    DbSet<Horario> Horarios { get; }
    DbSet<Medicion> Mediciones { get; }

    // --- Entrenamiento ---
    DbSet<Objetivo> Objetivos { get; }
    DbSet<GrupoMuscular> GruposMusculares { get; }
    DbSet<Ejercicio> Ejercicios { get; }
    DbSet<Maquina> Maquinas { get; }
    DbSet<GimnasioMaquina> GimnasioMaquinas { get; }
    DbSet<Rutina> Rutinas { get; }
    DbSet<RutinaDetalle> RutinaDetalles { get; }
    DbSet<SocioRutina> SocioRutinas { get; }
    DbSet<SocioRutinaDetalle> SocioRutinaDetalles { get; }

    // --- Nutrición ---
    DbSet<AlimentoCategoria> AlimentosCategorias { get; }
    DbSet<Alimento> Alimentos { get; }
    DbSet<SocioDieta> SocioDietas { get; }
    DbSet<SocioDietaDetalle> SocioDietaDetalles { get; }

    // --- Sitio web y comunidad ---
    DbSet<CarouselItem> CarouselItems { get; }
    DbSet<FooterConfig> FooterConfigs { get; }
    DbSet<ForoPost> ForoPosts { get; }

    // --- Seguridad ---
    DbSet<TokenRecuperacion> TokensRecuperacion { get; }
    DbSet<TokenVerificacionEmail> TokensVerificacionEmail { get; }

    /// <summary>Confirma en la base todos los cambios pendientes de la unidad de trabajo.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
