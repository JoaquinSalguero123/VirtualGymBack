using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtualGymAssistant.Domain.Entities;

namespace VirtualGymAssistant.Infrastructure.Persistence.Configurations;

public class EjercicioConfiguration : IEntityTypeConfiguration<Ejercicio>
{
    public void Configure(EntityTypeBuilder<Ejercicio> entidad)
    {
        entidad.ToTable("Ejercicios");
        entidad.HasKey(e => e.Id);

        entidad.Property(e => e.Id).HasColumnName("ID_Ejercicio");
        entidad.Property(e => e.Nombre).HasColumnName("Nombre").HasMaxLength(100).IsRequired();
        entidad.Property(e => e.Descripcion).HasColumnName("Descripcion").HasMaxLength(500);
        entidad.Property(e => e.IdGrupo).HasColumnName("ID_Grupo");
        entidad.Property(e => e.VideoUrl).HasColumnName("VideoUrl").HasMaxLength(300);

        entidad.HasOne(e => e.Grupo)
            .WithMany(g => g.Ejercicios)
            .HasForeignKey(e => e.IdGrupo)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class MaquinaConfiguration : IEntityTypeConfiguration<Maquina>
{
    public void Configure(EntityTypeBuilder<Maquina> entidad)
    {
        entidad.ToTable("Maquinas");
        entidad.HasKey(m => m.Id);

        entidad.Property(m => m.Id).HasColumnName("ID_Maquina");
        entidad.Property(m => m.Nombre).HasColumnName("Nombre").HasMaxLength(100).IsRequired();
        entidad.Property(m => m.Descripcion).HasColumnName("Descripcion").HasMaxLength(255);
        entidad.Property(m => m.Foto).HasColumnName("Foto").HasMaxLength(500);
    }
}

public class GimnasioMaquinaConfiguration : IEntityTypeConfiguration<GimnasioMaquina>
{
    public void Configure(EntityTypeBuilder<GimnasioMaquina> entidad)
    {
        entidad.ToTable("GimnasioMaquinas");
        entidad.HasKey(g => g.Id);

        entidad.Property(g => g.Id).HasColumnName("ID_GimnasioMaquina");
        entidad.Property(g => g.IdGimnasio).HasColumnName("ID_Gimnasio");
        entidad.Property(g => g.IdMaquina).HasColumnName("ID_Maquina");
        entidad.Property(g => g.Cantidad).HasColumnName("Cantidad");
        entidad.Property(g => g.Estado).HasColumnName("Estado").HasMaxLength(30).IsRequired();

        entidad.HasOne(g => g.Gimnasio)
            .WithMany(x => x.Maquinas)
            .HasForeignKey(g => g.IdGimnasio)
            .OnDelete(DeleteBehavior.Cascade);

        entidad.HasOne(g => g.Maquina)
            .WithMany(m => m.EnSucursales)
            .HasForeignKey(g => g.IdMaquina)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RutinaConfiguration : IEntityTypeConfiguration<Rutina>
{
    public void Configure(EntityTypeBuilder<Rutina> entidad)
    {
        entidad.ToTable("Rutinas");
        entidad.HasKey(r => r.Id);

        entidad.Property(r => r.Id).HasColumnName("ID_Rutina");
        entidad.Property(r => r.IdOrganizacion).HasColumnName("ID_Organizacion");
        entidad.Property(r => r.Nombre).HasColumnName("Nombre").HasMaxLength(100).IsRequired();
        entidad.Property(r => r.Descripcion).HasColumnName("Descripcion").HasMaxLength(255);
        entidad.Property(r => r.Intensidad).HasColumnName("Intensidad").HasMaxLength(50);
        entidad.Property(r => r.IdObjetivo).HasColumnName("ID_Objetivo");
        entidad.Property(r => r.DiasPorSemana).HasColumnName("DiasPorSemana");
        entidad.Property(r => r.EjerciciosPorDia).HasColumnName("EjerciciosPorDia");
        entidad.Property(r => r.Activa).HasColumnName("Activa");

        entidad.HasOne(r => r.Organizacion)
            .WithMany(o => o.Rutinas)
            .HasForeignKey(r => r.IdOrganizacion)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(r => r.Objetivo)
            .WithMany(o => o.Rutinas)
            .HasForeignKey(r => r.IdObjetivo)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RutinaDetalleConfiguration : IEntityTypeConfiguration<RutinaDetalle>
{
    public void Configure(EntityTypeBuilder<RutinaDetalle> entidad)
    {
        entidad.ToTable("RutinaDetalle");
        entidad.HasKey(d => d.Id);

        entidad.Property(d => d.Id).HasColumnName("ID_RutinaDetalle");
        entidad.Property(d => d.IdRutina).HasColumnName("ID_Rutina");
        entidad.Property(d => d.Dia).HasColumnName("Dia");
        entidad.Property(d => d.Orden).HasColumnName("Orden");
        entidad.Property(d => d.IdEjercicio).HasColumnName("ID_Ejercicio");
        entidad.Property(d => d.Series).HasColumnName("Series");
        entidad.Property(d => d.Repeticiones).HasColumnName("Repeticiones").HasMaxLength(20);
        entidad.Property(d => d.DescansoSeg).HasColumnName("DescansoSeg");

        entidad.HasOne(d => d.Rutina)
            .WithMany(r => r.Detalle)
            .HasForeignKey(d => d.IdRutina)
            .OnDelete(DeleteBehavior.Cascade);

        entidad.HasOne(d => d.Ejercicio)
            .WithMany(e => e.EnPlantillas)
            .HasForeignKey(d => d.IdEjercicio)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SocioRutinaConfiguration : IEntityTypeConfiguration<SocioRutina>
{
    public void Configure(EntityTypeBuilder<SocioRutina> entidad)
    {
        entidad.ToTable("SocioRutinas");
        entidad.HasKey(r => r.Id);

        entidad.Property(r => r.Id).HasColumnName("ID_SocioRutina");
        entidad.Property(r => r.IdSocio).HasColumnName("ID_Socio");
        entidad.Property(r => r.IdRutina).HasColumnName("ID_Rutina");
        entidad.Property(r => r.IdEntrenador).HasColumnName("ID_Entrenador");
        entidad.Property(r => r.Nombre).HasColumnName("Nombre").HasMaxLength(100).IsRequired();
        entidad.Property(r => r.FechaInicio).HasColumnName("FechaInicio");
        entidad.Property(r => r.FechaFin).HasColumnName("FechaFin");
        entidad.Property(r => r.Activa).HasColumnName("Activa");

        entidad.HasOne(r => r.Socio)
            .WithMany(s => s.Rutinas)
            .HasForeignKey(r => r.IdSocio)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(r => r.Plantilla)
            .WithMany()
            .HasForeignKey(r => r.IdRutina)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(r => r.Entrenador)
            .WithMany()
            .HasForeignKey(r => r.IdEntrenador)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SocioRutinaDetalleConfiguration : IEntityTypeConfiguration<SocioRutinaDetalle>
{
    public void Configure(EntityTypeBuilder<SocioRutinaDetalle> entidad)
    {
        entidad.ToTable("SocioRutinaDetalle");
        entidad.HasKey(d => d.Id);

        entidad.Property(d => d.Id).HasColumnName("ID_SocioRutinaDetalle");
        entidad.Property(d => d.IdSocioRutina).HasColumnName("ID_SocioRutina");
        entidad.Property(d => d.Dia).HasColumnName("Dia");
        entidad.Property(d => d.Orden).HasColumnName("Orden");
        entidad.Property(d => d.IdEjercicio).HasColumnName("ID_Ejercicio");
        entidad.Property(d => d.Series).HasColumnName("Series");
        entidad.Property(d => d.Repeticiones).HasColumnName("Repeticiones").HasMaxLength(20);
        entidad.Property(d => d.DescansoSeg).HasColumnName("DescansoSeg");
        entidad.Property(d => d.Notas).HasColumnName("Notas").HasMaxLength(255);

        entidad.HasOne(d => d.SocioRutina)
            .WithMany(r => r.Detalle)
            .HasForeignKey(d => d.IdSocioRutina)
            .OnDelete(DeleteBehavior.Cascade);

        entidad.HasOne(d => d.Ejercicio)
            .WithMany(e => e.EnRutinasDeSocios)
            .HasForeignKey(d => d.IdEjercicio)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
