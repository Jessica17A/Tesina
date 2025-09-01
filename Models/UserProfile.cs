using Microsoft.AspNetCore.Identity;

namespace WebGradu.Models
{
    public class UserProfile
    {
        public string Id { get; set; } // Esta es la clave primaria

        public string UserId { get; set; } // Relación con AspNetUsers
        public string FirstName { get; set; }
        public string LastName { get; set; }

        // Relación con IdentityUser
        public virtual IdentityUser User { get; set; }
    }
}
