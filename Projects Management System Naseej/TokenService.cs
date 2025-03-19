using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

public class TokenService
{
    private readonly string _secretKey;

    public TokenService(string secretKey)
    {
        _secretKey = secretKey;
    }

    public ClaimsPrincipal ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = "YourIssuer",
                ValidateAudience = true,
                ValidAudience = "YourAudience",
                // Set this to true if you want to validate the lifetime of the token
                ValidateLifetime = true,
                // Set this to the maximum allowable clock skew - five minutes is a common value
                ClockSkew = TimeSpan.FromMinutes(5)
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            return new ClaimsPrincipal(new ClaimsIdentity(jwtToken.Claims));
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }
}