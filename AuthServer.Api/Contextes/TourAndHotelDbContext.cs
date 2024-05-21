using AuthServer.Api.Models;
using Microsoft.EntityFrameworkCore;

public class TourAndHotelDbContext : DbContext
{
    public TourAndHotelDbContext(DbContextOptions<TourAndHotelDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }
    public DbSet<Images> Images { get; set; }
    public DbSet<Hotel> Hotels { get; set; }
    public DbSet<Tour> Tours { get; set; }
    public DbSet<TourHotel> ToursWithHotels { get; set; }
    public DbSet<BookedTours> BookedTours { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tour>()
            .HasMany(t => t.Images)
            .WithOne()
            .HasForeignKey(i => i.TourId); 

        modelBuilder.Entity<Hotel>()
            .HasMany(h => h.Images)
            .WithOne()
            .HasForeignKey(i => i.HotelId);

        modelBuilder.Entity<Tour>()
            .HasMany(t => t.TourHotels)
            .WithOne(th => th.Tour)
            .HasForeignKey(th => th.TourId); 

        modelBuilder.Entity<Hotel>()
            .HasMany(h => h.TourHotels)
            .WithOne(th => th.Hotel)
            .HasForeignKey(th => th.HotelId); 

        modelBuilder.Entity<TourHotel>()
            .HasKey(th => new { th.TourId, th.HotelId }); 
    }
}
