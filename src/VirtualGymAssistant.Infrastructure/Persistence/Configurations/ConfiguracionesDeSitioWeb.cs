using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtualGymAssistant.Domain.Entities;

namespace VirtualGymAssistant.Infrastructure.Persistence.Configurations;

public class HorarioConfiguration : IEntityTypeConfiguration<Horario>
{
    public void Configure(EntityTypeBuilder<Horario> entidad)
    {
        entidad.ToTable("Horarios");
        entidad.HasKey(h => h.Id);

        entidad.Property(h => h.Id).HasColumnName("ID_Horario");
        entidad.Property(h => h.IdGimnasio).HasColumnName("ID_Gimnasio");
        entidad.Property(h => h.DiaSemana).HasColumnName("DiaSemana");
        entidad.Property(h => h.HoraApertura).HasColumnName("HoraApertura");
        entidad.Property(h => h.HoraCierre).HasColumnName("HoraCierre");
        entidad.Property(h => h.Orden).HasColumnName("Orden");

        entidad.HasOne(h => h.Gimnasio)
            .WithMany(g => g.Horarios)
            .HasForeignKey(h => h.IdGimnasio)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CarouselItemConfiguration : IEntityTypeConfiguration<CarouselItem>
{
    public void Configure(EntityTypeBuilder<CarouselItem> entidad)
    {
        entidad.ToTable("CarouselItems");
        entidad.HasKey(c => c.Id);

        entidad.Property(c => c.Id).HasColumnName("ID_Carousel");
        entidad.Property(c => c.IdOrganizacion).HasColumnName("ID_Organizacion");
        entidad.Property(c => c.IdGimnasio).HasColumnName("ID_Gimnasio");
        entidad.Property(c => c.Tipo).HasColumnName("Tipo").HasMaxLength(50);
        entidad.Property(c => c.Titulo).HasColumnName("Titulo").HasMaxLength(100);
        entidad.Property(c => c.Descripcion).HasColumnName("Descripcion").HasMaxLength(255);
        entidad.Property(c => c.Imagen).HasColumnName("Imagen").HasMaxLength(300);
        entidad.Property(c => c.Orden).HasColumnName("Orden");
        entidad.Property(c => c.Activo).HasColumnName("Activo");

        entidad.HasOne(c => c.Organizacion)
            .WithMany(o => o.ItemsDeCarousel)
            .HasForeignKey(c => c.IdOrganizacion)
            .OnDelete(DeleteBehavior.Cascade);

        entidad.HasOne(c => c.Gimnasio)
            .WithMany()
            .HasForeignKey(c => c.IdGimnasio)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class FooterConfigConfiguration : IEntityTypeConfiguration<FooterConfig>
{
    public void Configure(EntityTypeBuilder<FooterConfig> entidad)
    {
        entidad.ToTable("FooterConfig");
        entidad.HasKey(f => f.Id);

        entidad.Property(f => f.Id).HasColumnName("ID_Footer");
        entidad.Property(f => f.IdOrganizacion).HasColumnName("ID_Organizacion");
        entidad.Property(f => f.Seccion).HasColumnName("Seccion").HasMaxLength(50).IsRequired();
        entidad.Property(f => f.Clave).HasColumnName("Clave").HasMaxLength(50).IsRequired();
        entidad.Property(f => f.Valor).HasColumnName("Valor").HasMaxLength(255);
        entidad.Property(f => f.Orden).HasColumnName("Orden");

        entidad.HasOne(f => f.Organizacion)
            .WithMany(o => o.ConfiguracionDePie)
            .HasForeignKey(f => f.IdOrganizacion)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ForoPostConfiguration : IEntityTypeConfiguration<ForoPost>
{
    public void Configure(EntityTypeBuilder<ForoPost> entidad)
    {
        entidad.ToTable("ForoPosts");
        entidad.HasKey(p => p.Id);

        entidad.Property(p => p.Id).HasColumnName("ID_Post");
        entidad.Property(p => p.IdOrganizacion).HasColumnName("ID_Organizacion");
        entidad.Property(p => p.IdGimnasio).HasColumnName("ID_Gimnasio");
        entidad.Property(p => p.IdUsuario).HasColumnName("ID_Usuario");
        entidad.Property(p => p.IdPostPadre).HasColumnName("ID_PostPadre");
        entidad.Property(p => p.Contenido).HasColumnName("Contenido");
        entidad.Property(p => p.Archivo).HasColumnName("Archivo").HasMaxLength(300);
        entidad.Property(p => p.Fecha).HasColumnName("Fecha");

        entidad.HasOne(p => p.Organizacion)
            .WithMany(o => o.PublicacionesDeForo)
            .HasForeignKey(p => p.IdOrganizacion)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(p => p.Gimnasio)
            .WithMany()
            .HasForeignKey(p => p.IdGimnasio)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(p => p.Usuario)
            .WithMany()
            .HasForeignKey(p => p.IdUsuario)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(p => p.PostPadre)
            .WithMany(p => p.Respuestas)
            .HasForeignKey(p => p.IdPostPadre)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
