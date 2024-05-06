namespace AuthServer.Api.Models
{
    public class TourHotel
    {
            public int TourId { get; set; }
            public Tour ?Tour { get; set; }
            public int HotelId { get; set; }
            public Hotel? Hotel { get; set; }
       
    }
}
