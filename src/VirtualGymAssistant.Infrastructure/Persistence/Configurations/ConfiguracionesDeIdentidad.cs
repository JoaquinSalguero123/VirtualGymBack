using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtualGymAssistant.Domain.Entities;

namespace VirtualGymAssistant.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> entidad)
    {
        entidad.ToTable("Usuarios");
        entidad.HasKey(u => u.Id);

        entidad.Property(u => u.Id).HasColumnName("ID_Usuario");
        entidad.Property(u => u.Username).HasColumnName("Username").HasMaxLength(50).IsRequired();
        entidad.Property(u => u.Mail).HasColumnName("Mail").HasMaxLength(254).IsRequired();
        entidad.Property(u => u.Nombre).HasColumnName("Nombre").HasMaxLength(60);
        entidad.Property(u => u.Apellido).HasColumnName("Apellido").HasMaxLength(60);
        entidad.Property(u => u.Telefono).HasColumnName("Telefono").HasMaxLength(30);
        entidad.Property(u => u.Clave).HasColumnName("Clave").HasMaxLength(255).IsRequired();
        entidad.Property(u => u.Activo).HasColumnName("Activo");
        entidad.Property(u => u.EmailVerificado).HasColumnName("EmailVerificado");
        entidad.Property(u => u.FechaAlta).HasColumnName("FechaAlta");

        // NombreCompleto es una propiedad calculada de C# (sólo tiene getter):
        // EF la ignora automáticamente y no busca ninguna columna para ella.
    }
}

public class OrganizacionConfiguration : IEntityTypeConfiguration<Organizacion>
{
    public void Configure(EntityTypeBuilder<Organizacion> entidad)
    {
        entidad.ToTable("Organizaciones");
        entidad.HasKey(o => o.Id);

        entidad.Property(o => o.Id).HasColumnName("ID_Organizacion");
        entidad.Property(o => o.Nombre).HasColumnName("Nombre").HasMaxLength(100).IsRequired();
        entidad.Property(o => o.RazonSocial).HasColumnName("RazonSocial").HasMaxLength(150);
        entidad.Property(o => o.Cuit).HasColumnName("CUIT").HasMaxLength(13).IsUnicode(false);
        entidad.Property(o => o.IdDuenio).HasColumnName("ID_Duenio");
        entidad.Property(o => o.Activo).HasColumnName("Activo");
        entidad.Property(o => o.FechaAlta).HasColumnName("FechaAlta");

        entidad.HasOne(o => o.Duenio)
            .WithMany(u => u.OrganizacionesQuePosee)
            .HasForeignKey(o => o.IdDuenio)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class GimnasioConfiguration : IEntityTypeConfiguration<Gimnasio>
{
    public void Configure(EntityTypeBuilder<Gimnasio> entidad)
    {
        entidad.ToTable("Gimnasios");
        entidad.HasKey(g => g.Id);

        entidad.Property(g => g.Id).HasColumnName("ID_Gimnasio");
        entidad.Property(g => g.IdOrganizacion).HasColumnName("ID_Organizacion");
        entidad.Property(g => g.Nombre).HasColumnName("Nombre").HasMaxLength(80).IsRequired();
        entidad.Property(g => g.Direccion).HasColumnName("Direccion").HasMaxLength(150).IsRequired();
        entidad.Property(g => g.Latitud).HasColumnName("Latitud").HasPrecision(9, 6);
        entidad.Property(g => g.Longitud).HasColumnName("Longitud").HasPrecision(9, 6);
        entidad.Property(g => g.IdZona).HasColumnName("ID_Zona");
        entidad.Property(g => g.Telefono).HasColumnName("Telefono").HasMaxLength(30);
        entidad.Property(g => g.Email).HasColumnName("Email").HasMaxLength(254);
        entidad.Property(g => g.EsCasaMatriz).HasColumnName("EsCasaMatriz");
        entidad.Property(g => g.Activo).HasColumnName("Activo");
        entidad.Property(g => g.FechaAlta).HasColumnName("FechaAlta");

        entidad.HasOne(g => g.Organizacion)
            .WithMany(o => o.Gimnasios)
            .HasForeignKey(g => g.IdOrganizacion)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(g => g.Zona)
            .WithMany(z => z.Gimnasios)
            .HasForeignKey(g => g.IdZona)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class UsuarioGimnasioRolConfiguration : IEntityTypeConfiguration<UsuarioGimnasioRol>
{
    public void Configure(EntityTypeBuilder<UsuarioGimnasioRol> entidad)
    {
        entidad.ToTable("UsuarioGimnasioRol");
        entidad.HasKey(u => u.Id);

        entidad.Property(u => u.Id).HasColumnName("ID_UGR");
        entidad.Property(u => u.IdUsuario).HasColumnName("ID_Usuario");
        entidad.Property(u => u.IdGimnasio).HasColumnName("ID_Gimnasio");
        entidad.Property(u => u.IdRol).HasColumnName("ID_Rol");
        entidad.Property(u => u.FechaDesde).HasColumnName("FechaDesde");
        entidad.Property(u => u.FechaHasta).HasColumnName("FechaHasta");
        entidad.Property(u => u.Activo).HasColumnName("Activo");

        entidad.HasOne(u => u.Usuario)
            .WithMany(x => x.RolesPorSucursal)
            .HasForeignKey(u => u.IdUsuario)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(u => u.Gimnasio)
            .WithMany(g => g.Personal)
            .HasForeignKey(u => u.IdGimnasio)
            .OnDelete(DeleteBehavior.Restrict);

        entidad.HasOne(u => u.Rol)
            .WithMany(r => r.Asignaciones)
            .HasForeignKey(u => u.IdRol)
            .OnDelete(DeleteBehavior.Restrict);

        // La base tiene un índice único sobre (ID_Usuario, ID_Gimnasio, ID_Rol): por eso
        // al cambiar de rol se reactiva la fila existente en lugar de insertar otra.
    }
}

public class TokenRecuperacionConfiguration : IEntityTypeConfiguration<TokenRecuperacion>
{
    public void Configure(EntityTypeBuilder<TokenRecuperacion> entidad)
    {
        entidad.ToTable("TokensRecuperacion");
        entidad.HasKey(t => t.Id);

        entidad.Property(t => t.Id).HasColumnName("ID_Token");
        entidad.Property(t => t.IdUsuario).HasColumnName("ID_Usuario");
        entidad.Property(t => t.Token).HasColumnName("Token").HasMaxLength(255);
        entidad.Property(t => t.Codigo).HasColumnName("Codigo").HasMaxLength(10).IsUnicode(false);
        entidad.Property(t => t.Canal).HasColumnName("Canal").HasMaxLength(10).IsRequired();
        entidad.Property(t => t.Expira).HasColumnName("Expira");
        entidad.Property(t => t.Usado).HasColumnName("Usado");
        entidad.Property(t => t.FechaCreacion).HasColumnName("FechaCreacion");

        entidad.HasOne(t => t.Usuario)
            .WithMany(u => u.TokensDeRecuperacion)
            .HasForeignKey(t => t.IdUsuario)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TokenVerificacionEmailConfiguration : IEntityTypeConfiguration<TokenVerificacionEmail>
{
    public void Configure(EntityTypeBuilder<TokenVerificacionEmail> entidad)
    {
        entidad.ToTable("TokensVerificacionEmail");
        entidad.HasKey(t => t.Id);

        entidad.Property(t => t.Id).HasColumnName("ID_Token");
        entidad.Property(t => t.IdUsuario).HasColumnName("ID_Usuario");
        entidad.Property(t => t.Codigo).HasColumnName("Codigo").HasMaxLength(10).IsUnicode(false).IsRequired();
        entidad.Property(t => t.Expira).HasColumnName("Expira");
        entidad.Property(t => t.Usado).HasColumnName("Usado");
        entidad.Property(t => t.FechaCreacion).HasColumnName("FechaCreacion");

        entidad.HasOne(t => t.Usuario)
            .WithMany(u => u.TokensDeVerificacion)
            .HasForeignKey(t => t.IdUsuario)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
