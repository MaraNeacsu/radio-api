using Microsoft.AspNetCore.Identity;

namespace WebAPI.Models
{
    public class AplicationUser : IdentityUser
    {
       
        public Contributor? Contributor { get; set; }
    }
}
