using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtualGymAssistant.Domain.Entities;

namespace VirtualGymAssistant.Infrastructure.Persistence.Configurations;

public class AlimentoConfiguration : IEntityTypeConfiguration<Alimento>
{
    public void Configure(EntityTypeBuilder<Alimento> entidad)
    {
        entidad.ToTable("Alimentos");
        entidad.HasKey(a => a.Id);

        entidad.Property(a => a.Id).HasColumnName("ID_Alimento");
        entidad.Property(a => a.Nombre).HasColumnName("Nombre").HasMaxLength(100).IsRequired();
        entidad.Property(a => a.Calorias).HasColumnName("Calorias").HasPrecision(7, 2);
        entidad.Property(a => a.Proteinas).HasColumnName("Proteinas").HasPrecision(6, 2);
        entidad.Property(a => a.Carbohidratos).HasColumnName("Carbohidratos").HasPrecision(6, 2);
        entidad.Property(a => a.Grasas).HasColumnName("Grasas").HasPrecision(6, 2);
        entidad.Property(a => a.Descripcion).HasColumnName("Descripcion").HasMaxLength(255);
        entidad.Property(a => a.IdCategoria).HasColumnName("ID_Categoria");
        entidad.Property(a => a.IdObjetivo).HasColumnName("ID_Objetivo");

        entidad.HasOne(a => a.Categoria)
            .WithMany(c => c.Alimentos)
            .HasForeignKey(a => a.IdCategoria)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(a => a.Objetivo)
            .WithMany(o => o.Alimentos)
            .HasForeignKey(a => a.IdObjetivo)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SocioDietaConfiguration : IEntityTypeConfiguration<SocioDieta>
{
    public void Configure(EntityTypeBuilder<SocioDieta> entidad)
    {
        entidad.ToTable("SocioDietas");
        entidad.HasKey(d => d.Id);

        entidad.Property(d => d.Id).HasColumnName("ID_SocioDieta");
        entidad.Property(d => d.IdSocio).HasColumnName("ID_Socio");
        entidad.Property(d => d.IdNutricionista).HasColumnName("ID_Nutricionista");
        entidad.Property(d => d.Nombre).HasColumnName("Nombre").HasMaxLength(100).IsRequired();
        entidad.Property(d => d.IdObjetivo).HasColumnName("ID_Objetivo");
        entidad.Property(d => d.FechaInicio).HasColumnName("FechaInicio");
        entidad.Property(d => d.FechaFin).HasColumnName("FechaFin");
        entidad.Property(d => d.Activa).HasColumnName("Activa");

        entidad.HasOne(d => d.Socio)
            .WithMany(s => s.Dietas)
            .HasForeignKey(d => d.IdSocio)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(d => d.Nutricionista)
            .WithMany()
            .HasForeignKey(d => d.IdNutricionista)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(d => d.Objetivo)
            .WithMany()
            .HasForeignKey(d => d.IdObjetivo)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SocioDietaDetalleConfiguration : IEntityTypeConfiguration<SocioDietaDetalle>
{
    public void Configure(EntityTypeBuilder<SocioDietaDetalle> entidad)
    {
        entidad.ToTable("SocioDietaDetalle");
        entidad.HasKey(d => d.Id);

        entidad.Property(d => d.Id).HasColumnName("ID_SocioDietaDetalle");
        entidad.Property(d => d.IdSocioDieta).HasColumnName("ID_SocioDieta");
        entidad.Property(d => d.Dia).HasColumnName("Dia").HasMaxLength(20).IsRequired();
        entidad.Property(d => d.Categoria).HasColumnName("Categoria").HasMaxLength(50).IsRequired();
        entidad.Property(d => d.Alimento).HasColumnName("Alimento").HasMaxLength(150).IsRequired();
        entidad.Property(d => d.IdAlimento).HasColumnName("ID_Alimento");

        entidad.HasOne(d => d.SocioDieta)
            .WithMany(s => s.Detalle)
            .HasForeignKey(d => d.IdSocioDieta)
            .OnDelete(DeleteBehavior.Cascade);

        entidad.HasOne(d => d.AlimentoDelCatalogo)
            .WithMany()
            .HasForeignKey(d => d.IdAlimento)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
