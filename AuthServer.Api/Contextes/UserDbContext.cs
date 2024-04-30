using AuthServer.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Api.Contextes
{
    public class UserDbContext : IdentityDbContext<ExtendedIdentityUser>
    {
        public UserDbContext(DbContextOptions<UserDbContext> options):base(options)
        {
            Database.EnsureCreated();
        }
        public DbSet<ExtendedIdentityUser> ExtendedIdentityUsers { get; set; }
    }
}
