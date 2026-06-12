using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;

namespace GotsThorlabs.NodesApi
{
    public class NodeLogin
    {
        public NodeLogin(WebApplication App)
        {
            App.MapPost("/api/login", async (LoginDTO sesionuser, IAuthService authService) =>
            {
                var result = authService.Login(sesionuser);
                if (result.Token is not null)
                    return Results.Ok(result.Token);
                return Results.BadRequest(result);
            });
        }
    }
}