using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Domain.Entities;

namespace VirtualGymAssistant.Infrastructure.Persistence;

/// <summary>
/// Contexto de Entity Framework Core del sistema.
///
/// IMPORTANTE: el esquema de la base lo definen los scripts SQL del proyecto
/// (01_EsquemaBD.sql y 02_Contenido.sql), no las migraciones de EF. Este contexto se
/// limita a MAPEAR ese esquema ya existente: por eso cada entidad declara explícitamente
/// su tabla y sus columnas en las clases de configuración.
/// </summary>
public class VirtualGymDbContext : DbContext, IVirtualGymDbContext
{
    public VirtualGymDbContext(DbContextOptions<VirtualGymDbContext> options) : base(options) { }

    // --- Identidad y organización ---
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<UsuarioGimnasioRol> UsuarioGimnasioRoles => Set<UsuarioGimnasioRol>();
    public DbSet<Organizacion> Organizaciones => Set<Organizacion>();
    public DbSet<Gimnasio> Gimnasios => Set<Gimnasio>();
    public DbSet<Zona> Zonas => Set<Zona>();

    // --- Socios ---
    public DbSet<Socio> Socios => Set<Socio>();
    public DbSet<SocioEstado> SocioEstados => Set<SocioEstado>();
    public DbSet<SocioAccesoGimnasio> SocioAccesosGimnasio => Set<SocioAccesoGimnasio>();

    // --- Comercial ---
    public DbSet<Plan> Planes => Set<Plan>();
    public DbSet<Membresia> Membresias => Set<Membresia>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<PagoEstado> PagoEstados => Set<PagoEstado>();
    public DbSet<FormaPago> FormasPago => Set<FormaPago>();

    // --- Operación diaria ---
    public DbSet<Asistencia> Asistencias => Set<Asistencia>();
    public DbSet<Horario> Horarios => Set<Horario>();
    public DbSet<Medicion> Mediciones => Set<Medicion>();

    // --- Entrenamiento ---
    public DbSet<Objetivo> Objetivos => Set<Objetivo>();
    public DbSet<GrupoMuscular> GruposMusculares => Set<GrupoMuscular>();
    public DbSet<Ejercicio> Ejercicios => Set<Ejercicio>();
    public DbSet<Maquina> Maquinas => Set<Maquina>();
    public DbSet<GimnasioMaquina> GimnasioMaquinas => Set<GimnasioMaquina>();
    public DbSet<Rutina> Rutinas => Set<Rutina>();
    public DbSet<RutinaDetalle> RutinaDetalles => Set<RutinaDetalle>();
    public DbSet<SocioRutina> SocioRutinas => Set<SocioRutina>();
    public DbSet<SocioRutinaDetalle> SocioRutinaDetalles => Set<SocioRutinaDetalle>();

    // --- Nutrición ---
    public DbSet<AlimentoCategoria> AlimentosCategorias => Set<AlimentoCategoria>();
    public DbSet<Alimento> Alimentos => Set<Alimento>();
    public DbSet<SocioDieta> SocioDietas => Set<SocioDieta>();
    public DbSet<SocioDietaDetalle> SocioDietaDetalles => Set<SocioDietaDetalle>();

    // --- Sitio web y comunidad ---
    public DbSet<CarouselItem> CarouselItems => Set<CarouselItem>();
    public DbSet<FooterConfig> FooterConfigs => Set<FooterConfig>();
    public DbSet<ForoPost> ForoPosts => Set<ForoPost>();

    // --- Seguridad ---
    public DbSet<TokenRecuperacion> TokensRecuperacion => Set<TokenRecuperacion>();
    public DbSet<TokenVerificacionEmail> TokensVerificacionEmail => Set<TokenVerificacionEmail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Toma todas las clases que implementan IEntityTypeConfiguration<> de este ensamblado.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VirtualGymDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
