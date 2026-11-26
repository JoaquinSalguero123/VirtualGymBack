using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtualGymAssistant.Domain.Entities;

namespace VirtualGymAssistant.Infrastructure.Persistence.Configurations;

public class SocioConfiguration : IEntityTypeConfiguration<Socio>
{
    public void Configure(EntityTypeBuilder<Socio> entidad)
    {
        entidad.ToTable("Socios");
        entidad.HasKey(s => s.Id);

        entidad.Property(s => s.Id).HasColumnName("ID_Socio");
        entidad.Property(s => s.IdUsuario).HasColumnName("ID_Usuario");
        entidad.Property(s => s.IdGimnasio).HasColumnName("ID_Gimnasio");
        entidad.Property(s => s.NumeroSocio).HasColumnName("NumeroSocio");
        entidad.Property(s => s.IdEstado).HasColumnName("ID_Estado");
        entidad.Property(s => s.IdObjetivo).HasColumnName("ID_Objetivo");
        entidad.Property(s => s.FechaNacimiento).HasColumnName("FechaNacimiento");
        entidad.Property(s => s.FechaAlta).HasColumnName("FechaAlta");
        entidad.Property(s => s.FechaBaja).HasColumnName("FechaBaja");
        entidad.Property(s => s.Observaciones).HasColumnName("Observaciones").HasMaxLength(500);

        entidad.HasOne(s => s.Usuario)
            .WithMany(u => u.FichasDeSocio)
            .HasForeignKey(s => s.IdUsuario)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(s => s.Gimnasio)
            .WithMany(g => g.Socios)
            .HasForeignKey(s => s.IdGimnasio)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(s => s.Estado)
            .WithMany(e => e.Socios)
            .HasForeignKey(s => s.IdEstado)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(s => s.Objetivo)
            .WithMany(o => o.Socios)
            .HasForeignKey(s => s.IdObjetivo)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SocioAccesoGimnasioConfiguration : IEntityTypeConfiguration<SocioAccesoGimnasio>
{
    public void Configure(EntityTypeBuilder<SocioAccesoGimnasio> entidad)
    {
        entidad.ToTable("SocioAccesoGimnasio");

        // Clave compuesta: un socio puede tener acceso a varias sucursales.
        entidad.HasKey(a => new { a.IdSocio, a.IdGimnasio });

        entidad.Property(a => a.IdSocio).HasColumnName("ID_Socio");
        entidad.Property(a => a.IdGimnasio).HasColumnName("ID_Gimnasio");
        entidad.Property(a => a.FechaDesde).HasColumnName("FechaDesde");

        entidad.HasOne(a => a.Socio)
            .WithMany(s => s.AccesosAdicionales)
            .HasForeignKey(a => a.IdSocio)
            .OnDelete(DeleteBehavior.Cascade);

        entidad.HasOne(a => a.Gimnasio)
            .WithMany()
            .HasForeignKey(a => a.IdGimnasio)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AsistenciaConfiguration : IEntityTypeConfiguration<Asistencia>
{
    public void Configure(EntityTypeBuilder<Asistencia> entidad)
    {
        entidad.ToTable("Asistencias");
        entidad.HasKey(a => a.Id);

        entidad.Property(a => a.Id).HasColumnName("ID_Asistencia");
        entidad.Property(a => a.IdSocio).HasColumnName("ID_Socio");
        entidad.Property(a => a.IdGimnasio).HasColumnName("ID_Gimnasio");
        entidad.Property(a => a.FechaHoraEntrada).HasColumnName("FechaHoraEntrada");
        entidad.Property(a => a.FechaHoraSalida).HasColumnName("FechaHoraSalida");

        // Fecha es una columna calculada y persistida por SQL Server. Al declararla así,
        // EF la lee pero nunca intenta escribirla.
        entidad.Property(a => a.Fecha)
            .HasColumnName("Fecha")
            .HasComputedColumnSql("CAST([FechaHoraEntrada] AS date)", stored: true);

        entidad.HasOne(a => a.Socio)
            .WithMany(s => s.Asistencias)
            .HasForeignKey(a => a.IdSocio)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(a => a.Gimnasio)
            .WithMany(g => g.Asistencias)
            .HasForeignKey(a => a.IdGimnasio)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class MedicionConfiguration : IEntityTypeConfiguration<Medicion>
{
    public void Configure(EntityTypeBuilder<Medicion> entidad)
    {
        entidad.ToTable("Mediciones");
        entidad.HasKey(m => m.Id);

        entidad.Property(m => m.Id).HasColumnName("ID_Medicion");
        entidad.Property(m => m.IdSocio).HasColumnName("ID_Socio");
        entidad.Property(m => m.Fecha).HasColumnName("Fecha");
        entidad.Property(m => m.Peso).HasColumnName("Peso").HasPrecision(5, 2);
        entidad.Property(m => m.GrasaPct).HasColumnName("GrasaPct").HasPrecision(5, 2);
        entidad.Property(m => m.MusculoPct).HasColumnName("MusculoPct").HasPrecision(5, 2);
        entidad.Property(m => m.Cintura).HasColumnName("Cintura").HasPrecision(5, 2);
        entidad.Property(m => m.Cadera).HasColumnName("Cadera").HasPrecision(5, 2);
        entidad.Property(m => m.Pecho).HasColumnName("Pecho").HasPrecision(5, 2);
        entidad.Property(m => m.Brazo).HasColumnName("Brazo").HasPrecision(5, 2);
        entidad.Property(m => m.Pierna).HasColumnName("Pierna").HasPrecision(5, 2);
        entidad.Property(m => m.IdRegistradoPor).HasColumnName("ID_RegistradoPor");
        entidad.Property(m => m.Observaciones).HasColumnName("Observaciones").HasMaxLength(255);

        entidad.HasOne(m => m.Socio)
            .WithMany(s => s.Mediciones)
            .HasForeignKey(m => m.IdSocio)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(m => m.RegistradoPor)
            .WithMany()
            .HasForeignKey(m => m.IdRegistradoPor)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
