using AuthServer.Api.Contextes;
using AuthServer.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AuthServer.Api.Controllers
{
    public class TourController : Controller
    {
        private readonly TourAndHotelDbContext _context;
        public TourController(TourAndHotelDbContext context)
        {
            _context = context;
        }

        [HttpGet("ActualTours")]

        public async Task<IActionResult> ActualTours()
        {
            var result = await _context.Tours
                .Where(x => x.Discount > 5)
                .ToListAsync();
    
            return Ok(result);
               
        }

        [HttpGet("GetAllTours")]

        public async Task<IActionResult> GetAllTours()
        {
            var result = await _context.Tours
                .Select(t => new
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    Country = t.Country,
                    DepartureDate = t.DepartureDate,
                    ArrivalDate = t.ArrivalDate,
                    Star = t.Star,
                    Price = t.Price,
                    AvailableSeats = t.AvailableSeats,
                    Discount = t.Discount,
                    Images = t.Images.Path,
                    HotelId = t.Hotel.Id
                })
               .ToListAsync();

            return Ok(result);
        }
        public record FindParameters(string? Country, int? Passengers, DateOnly? DepartureDate, DateOnly? ArrivalDate);

        [HttpPost("FindTour")]
        public async Task<IActionResult> FindTour([FromBody] FindParameters parameters)
        {
            var query = _context.Tours.AsQueryable();


            if (!string.IsNullOrEmpty(parameters.Country))
            {
                query = query.Where(c => c.Country == parameters.Country);
            }


            if (parameters.Passengers.HasValue && parameters.Passengers > 0)
            {
                query = query.Where(p => p.AvailableSeats >= parameters.Passengers);
            }

            if (parameters.DepartureDate != null && parameters.DepartureDate.HasValue)
            {
                query = query.Where(dd => dd.DepartureDate >= parameters.DepartureDate);
            }

            if (parameters.ArrivalDate == null && !parameters.ArrivalDate.HasValue)
            {
                query = query.Where(dd => Convert.ToDateTime(parameters.ArrivalDate) == DateTime.Today.AddMonths(1));
            }


            if (parameters.ArrivalDate != null && parameters.ArrivalDate.HasValue)
            {
                query = query.Where(ad => ad.ArrivalDate <= parameters.ArrivalDate);
            }
            

            var result = await query
                 .Select(t => new
                 {
                     Id = t.Id,
                     Name = t.Name,
                     Description = t.Description,
                     Country = t.Country,
                     DepartureDate = t.DepartureDate,
                     ArrivalDate = t.ArrivalDate,
                     Star = t.Star,
                     Price = t.Price,
                     AvailableSeats = t.AvailableSeats,
                     Discount = t.Discount,
                     Images = t.Images.Path,
                     HotelId = t.Hotel.Id
                 })
               .ToListAsync();

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpPost("FindByCountry")]
        public async Task<IActionResult> FindByCountry([FromBody] string country)
        {
            var result = await _context.Tours
                .Where(c => c.Country == country)
                .ToArrayAsync();

            return Ok(result);

        }

        [HttpPost("FindByPrice")]
        public async Task<IActionResult> SortedByPrice([FromBody]double? lowRange, double? heightRange)
        {
            var query = _context.Tours.AsQueryable();

            if (lowRange.HasValue && lowRange != null)
            {
                query = query.Where(p => p.Price >= lowRange);
            }

            if (heightRange.HasValue && heightRange != null)
            {
                query = query.Where(p => p.Price <= heightRange);
            }

            var result = await query.ToListAsync();

            return Ok(result);
        }
        public record HotelInfo(int HostelId);
        [HttpPost("HotelInfo")]
        public async Task<IActionResult> HotelInformation([FromBody] HotelInfo info)
        {
            var hotel = await _context.Hotels
           .FirstOrDefaultAsync(h => h.Id == info.HostelId);

            if (hotel == null)
            {
                return NotFound();
            }

            return Ok(hotel);
        }

        [HttpPost("SortByRating")]

        public async Task<IActionResult> SortingByRating([FromBody] string ratingString)
        {
            
            ratingString = ratingString.Replace(',', '.');

            
            if (!double.TryParse(ratingString, NumberStyles.Any, CultureInfo.InvariantCulture, out double rating))
            {
                
                return BadRequest("Invalid rating format");
            }

            var result = await _context.Tours
                .Where(x => x.Star >= rating) 
                .OrderByDescending(x => x.Star) 
                .ToListAsync();

            return Ok(result);
        }

        public record BookingRequest(int TourId, int Persons);
        [HttpPatch("BookTour")]
        public async Task<IActionResult> BookATour([FromBody] BookingRequest bookingRequest)
        {
          
            var tour = await _context.Tours.FindAsync(bookingRequest.TourId);

            if (tour == null)
            {
                return NotFound("Тур не найден");
            }
                     
            if (bookingRequest.Persons > tour.AvailableSeats)
            {
                return BadRequest("Введенное количество персон превышает количество свободных мест");
            }
           
            double totalPrice = tour.Price * bookingRequest.Persons - ((tour.Price*tour.Discount)/100);
            tour.AvailableSeats -= bookingRequest.Persons;

            _context.Update(tour);
            await _context.SaveChangesAsync();

          
            return Ok($"Тур успешно забронирован для {bookingRequest.Persons} человек. Итоговая стоимость: {totalPrice}");
        }

        public record FindWithAllParameters
        (
            string? Country,
            int? Passengers,
            DateOnly? DepartureDate,
            DateOnly? ArrivalDate,
            double? TourRating,
            double? TourPrice,
            string? HotelCity,
            string? HotelType,
            string? HotelRating,
            bool? AllowChild,
            bool? FreeWifi,
            string? TypeOfNutrition

        );

        [HttpPost("SearchWithAllParameters")]
        public async Task<IActionResult> SearchWithAllParameters([FromBody] FindWithAllParameters parameters)
        {
            var query = from tour in _context.Tours
                        join hotel in _context.Hotels on tour.Hotel.Id equals hotel.Id
                        select new
                        {
                            Tour = tour,
                            Hotel = hotel
                        };

            if (parameters.Country != null)
            {
                query = query.Where(x => x.Tour.Country == parameters.Country);
            }
            if (parameters.Passengers.HasValue)
            {
                query = query.Where(x => x.Tour.AvailableSeats >= parameters.Passengers.Value);
            }
            if (parameters.DepartureDate.HasValue)
            {
                query = query.Where(x => x.Tour.DepartureDate >= parameters.DepartureDate.Value);
            }
            if (parameters.ArrivalDate.HasValue)
            {
                query = query.Where(x => x.Tour.ArrivalDate <= parameters.ArrivalDate.Value);
            }
            if (parameters.TourRating.HasValue)
            {
                query = query.Where(x => x.Tour.Star >= parameters.TourRating.Value);
            }
            if (parameters.TourPrice.HasValue)
            {
                query = query.Where(x => x.Tour.Price <= parameters.TourPrice.Value);
            }
            if (parameters.HotelCity != null)
            {
                query = query.Where(x => x.Hotel.City == parameters.HotelCity);
            }
            if (parameters.HotelType != null)
            {
                query = query.Where(x => x.Hotel.Type == parameters.HotelType);
            }
            if (parameters.HotelRating != null)
            {
                query = query.Where(x => x.Hotel.Star.ToString() == parameters.HotelRating);
            }
            if (parameters.AllowChild.HasValue)
            {
                query = query.Where(x => x.Hotel.AllowChild == parameters.AllowChild.Value);
            }
            if (parameters.FreeWifi.HasValue)
            {
                query = query.Where(x => x.Hotel.FreeWifi == parameters.FreeWifi.Value);
            }
            if (parameters.TypeOfNutrition != null)
            {
                query = query.Where(x => x.Hotel.TypeOfNutrition == parameters.TypeOfNutrition);
            }

            query = query.OrderBy(x => x.Tour.Country)
                         .ThenBy(x => x.Tour.AvailableSeats)
                         .ThenBy(x => x.Tour.DepartureDate);

            var result = await query.Select(t => new
            {
                Id = t.Tour.Id,
                Name = t.Tour,
                Description = t.Tour.Description,
                Country = t.Tour.Country,
                DepartureDate = t.Tour.DepartureDate,
                ArrivalDate = t.Tour.ArrivalDate,
                Star = t.Tour.Star,
                Price = t.Tour.Price,
                AvailableSeats = t.Tour.AvailableSeats,
                Discount = t.Tour.Discount,
                Images = t.Tour.Images.Path,
                HotelId = t.Hotel.Id,
                HottelName = t.Hotel.Name
            })
               .ToListAsync(); 
            return Ok(result);
        }
        
      



    }
}
