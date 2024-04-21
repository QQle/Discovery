using AuthServer.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Api.Contextes
{
    public class TourAndHotelDbContext : DbContext
    {
        public TourAndHotelDbContext(DbContextOptions<TourAndHotelDbContext> options) : base(options)
        {
            Database.EnsureCreated();
        }
        public DbSet<Images> Images { get; set; }   
        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<Tour> Tours { get; set; }
       
       

    }
}
