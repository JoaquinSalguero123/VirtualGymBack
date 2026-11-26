using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtualGymAssistant.Domain.Entities;

namespace VirtualGymAssistant.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de los catálogos generales. Son tablas chicas de sólo un nombre, así que sus
/// configuraciones van juntas: cambian por los mismos motivos y se leen de un vistazo.
/// </summary>
public class ZonaConfiguration : IEntityTypeConfiguration<Zona>
{
    public void Configure(EntityTypeBuilder<Zona> entidad)
    {
        entidad.ToTable("Zonas");
        entidad.HasKey(z => z.Id);

        entidad.Property(z => z.Id).HasColumnName("ID_Zona");
        entidad.Property(z => z.Nombre).HasColumnName("Nombre").HasMaxLength(80).IsRequired();
        entidad.Property(z => z.Localidad).HasColumnName("Localidad").HasMaxLength(80);
        entidad.Property(z => z.Provincia).HasColumnName("Provincia").HasMaxLength(80);
    }
}

public class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> entidad)
    {
        entidad.ToTable("Roles");
        entidad.HasKey(r => r.Id);

        entidad.Property(r => r.Id).HasColumnName("ID_Rol");
        entidad.Property(r => r.Nombre).HasColumnName("Nombre").HasMaxLength(50).IsRequired();
        entidad.Property(r => r.Descripcion).HasColumnName("Descripcion").HasMaxLength(150);
    }
}

public class SocioEstadoConfiguration : IEntityTypeConfiguration<SocioEstado>
{
    public void Configure(EntityTypeBuilder<SocioEstado> entidad)
    {
        entidad.ToTable("SocioEstados");
        entidad.HasKey(e => e.Id);

        entidad.Property(e => e.Id).HasColumnName("ID_Estado");
        entidad.Property(e => e.Nombre).HasColumnName("Nombre").HasMaxLength(30).IsRequired();
    }
}

public class PagoEstadoConfiguration : IEntityTypeConfiguration<PagoEstado>
{
    public void Configure(EntityTypeBuilder<PagoEstado> entidad)
    {
        entidad.ToTable("PagoEstados");
        entidad.HasKey(e => e.Id);

        entidad.Property(e => e.Id).HasColumnName("ID_EstadoPago");
        entidad.Property(e => e.Nombre).HasColumnName("Nombre").HasMaxLength(30).IsRequired();
    }
}

public class ObjetivoConfiguration : IEntityTypeConfiguration<Objetivo>
{
    public void Configure(EntityTypeBuilder<Objetivo> entidad)
    {
        entidad.ToTable("Objetivos");
        entidad.HasKey(o => o.Id);

        entidad.Property(o => o.Id).HasColumnName("ID_Objetivo");
        entidad.Property(o => o.Nombre).HasColumnName("Nombre").HasMaxLength(50).IsRequired();
    }
}

public class GrupoMuscularConfiguration : IEntityTypeConfiguration<GrupoMuscular>
{
    public void Configure(EntityTypeBuilder<GrupoMuscular> entidad)
    {
        entidad.ToTable("GruposMusculares");
        entidad.HasKey(g => g.Id);

        entidad.Property(g => g.Id).HasColumnName("ID_Grupo");
        entidad.Property(g => g.Nombre).HasColumnName("Nombre").HasMaxLength(50).IsRequired();
    }
}

public class AlimentoCategoriaConfiguration : IEntityTypeConfiguration<AlimentoCategoria>
{
    public void Configure(EntityTypeBuilder<AlimentoCategoria> entidad)
    {
        entidad.ToTable("AlimentosCategorias");
        entidad.HasKey(c => c.Id);

        entidad.Property(c => c.Id).HasColumnName("ID_Categoria");
        entidad.Property(c => c.Nombre).HasColumnName("Nombre").HasMaxLength(50).IsRequired();
    }
}

public class FormaPagoConfiguration : IEntityTypeConfiguration<FormaPago>
{
    public void Configure(EntityTypeBuilder<FormaPago> entidad)
    {
        entidad.ToTable("FormasPago");
        entidad.HasKey(f => f.Id);

        entidad.Property(f => f.Id).HasColumnName("ID_FormaPago");
        entidad.Property(f => f.Nombre).HasColumnName("Nombre").HasMaxLength(50).IsRequired();
        entidad.Property(f => f.Activo).HasColumnName("Activo");
    }
}
