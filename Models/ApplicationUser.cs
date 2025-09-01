using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }  // Permite valores nulos
    public string? LastName { get; set; }   // Permite valores nulos
}
