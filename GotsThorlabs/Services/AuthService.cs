using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GotsThorlabs.Services
{
    public class AuthService : IAuthService
    {
        private readonly ThorlabsDbContext _db;
        private readonly IConfiguration _configuration;

        public AuthService(ThorlabsDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public AuthResponseDTO Login(LoginDTO loginDto)
        {
            using var connection = ConnectionSqlite.CreateConnection();
            string loginString = "SELECT * FROM user WHERE (nickname = @User OR eMail = @User) AND password = @Password";
            var tableUsers = connection.Query<User>(loginString, new { User = loginDto.User, Password = loginDto.Password });

            if (tableUsers.Any())
            {
                var token = GenerateTokenJwt(tableUsers.FirstOrDefault().IdUser.ToString());
                return new AuthResponseDTO { Token = token };
            }

            return new AuthResponseDTO
            {
                Error = "Fallo en la validación",
                Description = "Intente de nuevo iniciar sesión"
            };
        }

        public object? GetUserData(string token)
        {
            var stream = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? token.Substring(7)
                : token;

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(stream) as JwtSecurityToken;

            if (jsonToken is null) return null;

            var userId = jsonToken.Payload["unique_name"]?.ToString();
            if (userId is null) return null;

            using var connection = ConnectionSqlite.CreateConnection();
            connection.Open();
            string query = "SELECT * FROM user WHERE idUser = @userId";
            var rows = connection.Query(query, new { userId }).ToList();
            return rows;
        }

        private string GenerateTokenJwt(string userId)
        {
            var secretKey = _configuration["CustomCOnfig:JWT_SECRET_KEY"];
            var audienceToken = _configuration["CustomCOnfig:JWT_AUDIENCE_TOKEN"];
            var issuerToken = _configuration["CustomCOnfig:JWT_ISSUER_TOKEN"];
            var expireTime = _configuration["CustomCOnfig:JWT_EXPIRE_MINUTES"];

            var securityKey = new SymmetricSecurityKey(Encoding.Default.GetBytes(secretKey));
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

            var claimsIdentity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, userId) });

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = tokenHandler.CreateJwtSecurityToken(
                audience: audienceToken,
                issuer: issuerToken,
                subject: claimsIdentity,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToInt32(expireTime)),
                signingCredentials: signingCredentials);

            return tokenHandler.WriteToken(jwtSecurityToken);
        }
    }
}