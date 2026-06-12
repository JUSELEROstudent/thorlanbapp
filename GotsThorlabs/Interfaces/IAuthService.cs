using GotsThorlabs.Models;

namespace GotsThorlabs.Interfaces
{
    public interface IAuthService
    {
        AuthResponseDTO Login(LoginDTO loginDto);
        object? GetUserData(string token);
    }
}