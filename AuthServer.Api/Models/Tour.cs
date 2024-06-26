﻿namespace AuthServer.Api.Models
{
    /// <summary>
    /// Модель сущности "Тур".
    /// </summary>
    public class Tour
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Country { get; set; }
        public DateOnly DepartureDate { get; set; }
        public DateOnly ArrivalDate { get; set; }
        public double? Star { get; set; }
        public double? Price { get; set; }
        public int? AvailableSeats { get; set; }
        public double? Discount { get; set; }
        public List<TourHotel>? TourHotels { get; set; }
        public List<Images>? Images { get; set; }
    }
}
