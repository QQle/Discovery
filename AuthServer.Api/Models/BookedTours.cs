namespace AuthServer.Api.Models
{
    public class BookedTours
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int TourId { get; set; }
        public Tour Tour { get; set; }
        public int HotelId { get; set; }
        public Hotel Hotel { get; set; }

    }
}
