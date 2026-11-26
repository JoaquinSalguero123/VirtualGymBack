using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Domain.Entities;

namespace VirtualGymAssistant.Infrastructure.Seguridad;

/// <summary>
/// Emite los tokens JWT que autentican al usuario en cada petición.
///
/// Decisión de diseño: el token lleva SÓLO la identidad (quién es), nunca los roles.
/// Como el rol depende de la sucursal en la que esté trabajando y puede cambiar en
/// cualquier momento, se resuelve contra la base en cada petición. Si los roles viajaran
/// firmados dentro del token, un cambio de permisos recién tendría efecto al vencer.
/// </summary>
public class ServicioDeTokensJwt : IServicioDeTokens
{
    private readonly OpcionesDeJwt _opciones;
    private readonly IProveedorDeFechaHora _reloj;

    public ServicioDeTokensJwt(IOptions<OpcionesDeJwt> opciones, IProveedorDeFechaHora reloj)
    {
        _opciones = opciones.Value;
        _reloj = reloj;
    }

    public (string Token, DateTime Expira) GenerarToken(Usuario usuario)
    {
        var expira = _reloj.Ahora.AddMinutes(_opciones.MinutosDeExpiracion);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Mail),
            new(ClaimTypes.Name, usuario.Username),

            // Identificador único del token: permite invalidarlo en el futuro si hiciera falta.
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrWhiteSpace(usuario.Nombre))
            claims.Add(new Claim(JwtRegisteredClaimNames.GivenName, usuario.Nombre));

        if (!string.IsNullOrWhiteSpace(usuario.Apellido))
            claims.Add(new Claim(JwtRegisteredClaimNames.FamilyName, usuario.Apellido));

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.ClaveSecreta));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opciones.Emisor,
            audience: _opciones.Audiencia,
            claims: claims,
            notBefore: _reloj.Ahora,
            expires: expira,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}
