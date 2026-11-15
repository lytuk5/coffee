namespace CafeDuaHau.Models;

public class CustomerAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserName { get; set; } = "";
    public string NormalizedUserName { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? ExternalProvider { get; set; }
    public string? ExternalSubject { get; set; }
    public string PasswordHash { get; set; } = "";
}
