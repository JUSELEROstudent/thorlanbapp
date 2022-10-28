using System;
using System.Configuration;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace apitest.Controllers
{/// <summary>
 /// JWT Token generator class using "secret-key"
 /// more info: https://self-issued.info/docs/draft-ietf-oauth-json-web-token.html
 /// </summary>
    internal static class TokenGenerator
    {
        
        public static string GenerateTokenJwt(string username)
        {
            var AppName = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            // appsetting for Token JWT
            var secretKey = AppName.GetSection("CustomCOnfig")["JWT_SECRET_KEY"];//se tienen que cambiar los valores para hacerlos secretos.
            var audienceToken = AppName.GetSection("CustomCOnfig")["JWT_AUDIENCE_TOKEN"];// se tienen que cambiar los valores para hacerlos secretos.
            var issuerToken = AppName.GetSection("CustomCOnfig")["JWT_ISSUER_TOKEN"];//se tienen que cambiar los valores para hacerlos secretos.
            var expireTime = AppName.GetSection("CustomCOnfig")["JWT_EXPIRE_MINUTES"];

            var securityKey = new SymmetricSecurityKey(System.Text.Encoding.Default.GetBytes(secretKey));
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

            // create a claimsIdentity
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) });

            // create token to the user
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtSecurityToken = tokenHandler.CreateJwtSecurityToken(
                audience: audienceToken,
                issuer: issuerToken,
                subject: claimsIdentity,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToInt32(expireTime)),
                signingCredentials: signingCredentials);

            var jwtTokenString = tokenHandler.WriteToken(jwtSecurityToken);
            return jwtTokenString;
        }
    }
}
