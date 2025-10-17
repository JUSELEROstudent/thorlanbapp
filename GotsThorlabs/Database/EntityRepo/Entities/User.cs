namespace GotsThorlabs.Database.EntityRepo.Entities;

public class User
{
    public int IdUser { get; set; }
    public string Nickname { get; set; } = default!;
    public string EMail { get; set; } = default!;
    public string Password { get; set; } = default!;
}