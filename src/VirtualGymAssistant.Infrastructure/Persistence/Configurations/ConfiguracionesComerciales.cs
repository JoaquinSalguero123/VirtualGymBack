using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtualGymAssistant.Domain.Entities;

namespace VirtualGymAssistant.Infrastructure.Persistence.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> entidad)
    {
        entidad.ToTable("Planes");
        entidad.HasKey(p => p.Id);

        entidad.Property(p => p.Id).HasColumnName("ID_Plan");
        entidad.Property(p => p.IdOrganizacion).HasColumnName("ID_Organizacion");
        entidad.Property(p => p.Nombre).HasColumnName("Nombre").HasMaxLength(100).IsRequired();
        entidad.Property(p => p.Descripcion).HasColumnName("Descripcion").HasMaxLength(255);
        entidad.Property(p => p.Precio).HasColumnName("Precio").HasPrecision(12, 2);
        entidad.Property(p => p.DuracionDias).HasColumnName("DuracionDias");
        entidad.Property(p => p.Nivel).HasColumnName("Nivel").HasMaxLength(50);
        entidad.Property(p => p.AccesoMultiSede).HasColumnName("AccesoMultiSede");
        entidad.Property(p => p.Activo).HasColumnName("Activo");

        entidad.HasOne(p => p.Organizacion)
            .WithMany(o => o.Planes)
            .HasForeignKey(p => p.IdOrganizacion)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class MembresiaConfiguration : IEntityTypeConfiguration<Membresia>
{
    public void Configure(EntityTypeBuilder<Membresia> entidad)
    {
        entidad.ToTable("Membresias");
        entidad.HasKey(m => m.Id);

        entidad.Property(m => m.Id).HasColumnName("ID_Membresia");
        entidad.Property(m => m.IdSocio).HasColumnName("ID_Socio");
        entidad.Property(m => m.IdPlan).HasColumnName("ID_Plan");
        entidad.Property(m => m.IdGimnasio).HasColumnName("ID_Gimnasio");
        entidad.Property(m => m.FechaInicio).HasColumnName("FechaInicio");
        entidad.Property(m => m.FechaFin).HasColumnName("FechaFin");
        entidad.Property(m => m.PrecioAcordado).HasColumnName("PrecioAcordado").HasPrecision(12, 2);
        entidad.Property(m => m.Activa).HasColumnName("Activa");

        entidad.HasOne(m => m.Socio)
            .WithMany(s => s.Membresias)
            .HasForeignKey(m => m.IdSocio)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(m => m.Plan)
            .WithMany(p => p.Membresias)
            .HasForeignKey(m => m.IdPlan)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(m => m.Gimnasio)
            .WithMany(g => g.Membresias)
            .HasForeignKey(m => m.IdGimnasio)
            .OnDelete(DeleteBehavior.Restrict);

        // La base tiene un índice único filtrado sobre ID_Socio con Activa = 1:
        // es lo que garantiza "una sola membresía activa por socio".
    }
}

public class PagoConfiguration : IEntityTypeConfiguration<Pago>
{
    public void Configure(EntityTypeBuilder<Pago> entidad)
    {
        entidad.ToTable("Pagos");
        entidad.HasKey(p => p.Id);

        entidad.Property(p => p.Id).HasColumnName("ID_Pago");
        entidad.Property(p => p.IdSocio).HasColumnName("ID_Socio");
        entidad.Property(p => p.IdGimnasio).HasColumnName("ID_Gimnasio");
        entidad.Property(p => p.IdMembresia).HasColumnName("ID_Membresia");
        entidad.Property(p => p.IdPlan).HasColumnName("ID_Plan");
        entidad.Property(p => p.IdFormaPago).HasColumnName("ID_FormaPago");
        entidad.Property(p => p.PeriodoAnio).HasColumnName("PeriodoAnio");
        entidad.Property(p => p.PeriodoMes).HasColumnName("PeriodoMes");
        entidad.Property(p => p.Monto).HasColumnName("Monto").HasPrecision(12, 2);
        entidad.Property(p => p.IdEstadoPago).HasColumnName("ID_EstadoPago");
        entidad.Property(p => p.FechaVencimiento).HasColumnName("FechaVencimiento");
        entidad.Property(p => p.FechaPago).HasColumnName("FechaPago");
        entidad.Property(p => p.FechaRegistro).HasColumnName("FechaRegistro");
        entidad.Property(p => p.Comprobante).HasColumnName("Comprobante").HasMaxLength(50);
        entidad.Property(p => p.Observaciones).HasColumnName("Observaciones").HasMaxLength(255);

        entidad.HasOne(p => p.Socio)
            .WithMany(s => s.Pagos)
            .HasForeignKey(p => p.IdSocio)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(p => p.Gimnasio)
            .WithMany(g => g.Pagos)
            .HasForeignKey(p => p.IdGimnasio)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(p => p.Membresia)
            .WithMany(m => m.Pagos)
            .HasForeignKey(p => p.IdMembresia)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(p => p.Plan)
            .WithMany(pl => pl.Pagos)
            .HasForeignKey(p => p.IdPlan)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(p => p.FormaPago)
            .WithMany(f => f.Pagos)
            .HasForeignKey(p => p.IdFormaPago)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(p => p.Estado)
            .WithMany(e => e.Pagos)
            .HasForeignKey(p => p.IdEstadoPago)
            .OnDelete(DeleteBehavior.Restrict);

        // La base tiene un índice único sobre (ID_Socio, PeriodoAnio, PeriodoMes):
        // es lo que garantiza "un solo pago por socio y período".
    }
}
