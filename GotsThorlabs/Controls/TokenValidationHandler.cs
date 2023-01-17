using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using apitest.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace apitest.Controllers
{

    public class Httpcontextentry : IAuthorizationRequirement
    {
        public Httpcontextentry(bool request) =>
            Requestentry = request;

        public bool Requestentry { get; }
    }
    ///<summary>
    ///   descripcion de los usuario que pueden ingresar de acuerdo a la politica de entrada que se va a implementar
    ///</summary>
     public class Authorizationadmin : IAuthorizationRequirement
    {
        public Authorizationadmin(int hierarchy) =>
            Hierarchyadmin = hierarchy;

    public int Hierarchyadmin { get; }
}

/// <summary>
/// Token validator for Authorization Request using a DelegatingHandler
/// </summary>
internal class TokenValidationHandler : AuthorizationHandler<Httpcontextentry>
    {
        // CONSTRUCTOR
        private readonly IHttpContextAccessor _httpContextAccessor; 


        protected static bool TryRetrieveToken(HttpContext request, out string token)
        {
            token = null;
            var authzHeaders = request.Request.Headers["Authorization"];
            //var otro = authzHeaders["Authorization"]
            //if (!request.Headers.TryGetValues("Authorization", out authzHeaders) || authzHeaders.Count() > 1)
            if (authzHeaders.Count() > 1)
            {
                return false;
            }          
            var bearerToken = authzHeaders.ElementAt(0);
            token = bearerToken.StartsWith("Bearer ") ? bearerToken.Substring(7) : bearerToken;
            return true;
           
        }

        //public Task<HttpStatusCode> SendAsync(HttpContext request, CancellationToken cancellationToken)  se quita el token de cancelacion
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, Httpcontextentry requirement)
        {
            var httpContextentry = _httpContextAccessor;
            // HttpStatusCode statusCode;
            string token;

            // determine whether a jwt exists or not
            if (!TryRetrieveToken(httpContextentry.HttpContext, out token))
            {

                return Task.CompletedTask;
            }

            try
            {
                var AppName = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
                var secretKey = AppName.GetSection("CustomCOnfig")["JWT_SECRET_KEY"];
                var audienceToken = AppName.GetSection("CustomCOnfig")["JWT_AUDIENCE_TOKEN"];
                var issuerToken = AppName.GetSection("CustomCOnfig")["JWT_ISSUER_TOKEN"];
                var securityKey = new SymmetricSecurityKey(System.Text.Encoding.Default.GetBytes(secretKey));

                SecurityToken securityToken;
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                TokenValidationParameters validationParameters = new TokenValidationParameters()
                {
                    ValidAudience = audienceToken,
                    ValidIssuer = issuerToken,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    LifetimeValidator = this.LifetimeValidator,
                    IssuerSigningKey = securityKey
                };

                // Extract and assign Current Principal and user
                Thread.CurrentPrincipal = tokenHandler.ValidateToken(token, validationParameters, out securityToken);
                //HttpContext.Current.User = tokenHandler.ValidateToken(token, validationParameters, out securityToken);
                context.Succeed(requirement);
            }
            catch (SecurityTokenValidationException)
            {
                return Task.CompletedTask;
            }
            catch (Exception)
            {
                return Task.CompletedTask;
            }
            return Task.CompletedTask;
            
        }

        public bool LifetimeValidator(DateTime? notBefore, DateTime? expires, SecurityToken securityToken, TokenValidationParameters validationParameters)
        {
            if (expires != null)
            {
                if (DateTime.UtcNow < expires) return true;
            }
            return false;
        }
    }
}

internal class TokenValidationHandlerr : AuthorizationHandler<Authorizationadmin>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, Authorizationadmin requirement)
    {
        //var requestactual = context.User.Claims;
        //if (true)  // requirement == requestactual.
        //{
        context.Succeed(requirement);
        //}
        
         return Task.CompletedTask;
    }
}
