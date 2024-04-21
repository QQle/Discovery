﻿using AuthServer.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Api.Contextes
{
    public class AuthDemoDbContext:IdentityDbContext
    {
        public AuthDemoDbContext(DbContextOptions<AuthDemoDbContext> options):base(options)
        {
            Database.EnsureCreated();
        }
        public DbSet<ExtendedIdentityUser> ExtendedIdentityUsers { get; set; }
    }
}
