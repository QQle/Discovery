namespace AuthServer.Api.Models
{
    public class Tour
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Country { get; set; } 
        public DateOnly DepartureDate { get; set; }
        public DateOnly ArrivalDate { get; set; }  
        public double Star { get; set; }
        public double Price { get; set; }
        public int AvailableSeats { get; set; }
        public double Discount { get; set; }
        public Hotel Hotel { get; set; }
        public Images Images { get; set; }

    }
}
