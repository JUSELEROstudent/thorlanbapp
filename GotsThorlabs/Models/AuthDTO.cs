namespace GotsThorlabs.Models
{
    public class LoginDTO
    {
        public string User { get; set; } = default!;
        public string Password { get; set; } = default!;
    }

    public class AuthResponseDTO
    {
        public string? Token { get; set; }
        public string? Error { get; set; }
        public string? Description { get; set; }
    }

    public class UserDataDTO
    {
        public int IdUser { get; set; }
        public string Nickname { get; set; } = default!;
        public string EMail { get; set; } = default!;
    }
}