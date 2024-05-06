using AuthServer.Api.Contextes;
using AuthServer.Api.Models;
using AuthServer.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq;

namespace AuthServer.Api.Controllers
{
    public class TourController : Controller 
    {
        private readonly TourAndHotelDbContext _tourAndHotelDbContext;
        private readonly UserDbContext _userDbContext;
        private readonly IEmailService _emailService;

        public TourController(TourAndHotelDbContext context, UserDbContext userDbContext, IEmailService emailService)
        {
            _tourAndHotelDbContext = context;
            _userDbContext = userDbContext;
            _emailService = emailService;
         
        }

        [HttpGet("ActualTours")]

        public async Task<IActionResult> ActualTours()
        {
            var result = await _tourAndHotelDbContext.Tours
                .Where(x => x.Discount > 5)
                .ToListAsync();

            return Ok(result);

        }

        [HttpGet("GetAllTours")]
        public async Task<IActionResult> GetAllTours()
        {
            var tours = await _tourAndHotelDbContext.Tours
                .Include(t => t.TourHotels)
                .ThenInclude(th => th.Hotel)
                .ToListAsync();

            var result = new List<object>();

            foreach (var tour in tours)
            {
                var availableSeats = tour.AvailableSeats;
                var basePrice = await GetTourPriceForHotelAsync((int)tour?.Id, 
                     tour.TourHotels.OrderBy(th => th?.Hotel?.Star)
                    .FirstOrDefault().HotelId);
                var hotelsInfo = new List<object>();

                foreach (var tourHotel in tour.TourHotels)
                {
                   var price = basePrice * (1 + tourHotel.Hotel.Star / 10);
                    availableSeats += tourHotel.Hotel.AvailablePerson; 
                    var hotelInfo = new
                    {
                        Id = tourHotel.Hotel.Id,
                        Name = tourHotel.Hotel.Name,
                        City = tourHotel.Hotel.City,
                        Type = tourHotel.Hotel.Type,
                        Description = tourHotel.Hotel.Description,
                        Star = tourHotel.Hotel.Star,
                        AllowChild = tourHotel.Hotel.AllowChild,
                        FreeWifi = tourHotel.Hotel.FreeWifi,
                        TypeOfNutrition = tourHotel.Hotel.TypeOfNutrition,
                        Images = tourHotel.Hotel.Images,
                        AvailablePerson = tourHotel.Hotel.AvailablePerson,
                        Price = price
                    };
                    hotelsInfo.Add(hotelInfo);
                }

                var tourInfo = new
                {
                    Id = tour.Id,
                    Name = tour.Name,
                    Description = tour.Description,
                    Country = tour.Country,
                    DepartureDate = tour.DepartureDate,
                    ArrivalDate = tour.ArrivalDate,
                    Star = tour.Star,
                    Price = basePrice,
                    AvailableSeats = tour.AvailableSeats,
                    Discount = tour.Discount,
                    Images = tour.Images
                };

                result.Add(new { Tour = tourInfo, Hotels = hotelsInfo });
            }


            return Ok(result);
        }




        private async Task<double> GetTourPriceForHotelAsync(int tourId, int hotelId)
        {
            var tour = await _tourAndHotelDbContext.Tours
                .Include(t => t.TourHotels)
                .ThenInclude(th => th.Hotel)
                .FirstOrDefaultAsync(t => t.Id == tourId);

            if (tour == null)
            {
               
                throw new ArgumentException($"Тур с Id {tourId} не найден.");
            }

            var tourHotel = tour?.TourHotels?.FirstOrDefault(th => th.HotelId == hotelId);

            if (tourHotel == null)
            {
                
                throw new ArgumentException($"Отель с id {hotelId} не найден для отеля с Id {tourId}.");
            }

            if (tour.Price == null)
            {
                
                throw new InvalidOperationException("Цена для тура не может быть рассчитана.");
            }

            double? basePrice = (double)tour.Price * (1 - (tour.Discount / 100));
            double? price = basePrice * (1 + tourHotel?.Hotel?.Star / 10);

            return (double)price;
        }



        public record FindParameters(string? Country, int? Passengers, DateOnly? DepartureDate, DateOnly? ArrivalDate);

        [HttpPost("FindTour")]
        public async Task<IActionResult> FindTour([FromBody] FindParameters parameters)
        {
            var query = _tourAndHotelDbContext.Tours.AsQueryable();


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
                     Images = t.Images,
                     Hotels = t.TourHotels.Select(th => new
                     {
                         Id = th.Hotel.Id,
                         Name = th.Hotel.Name,
                         City = th.Hotel.City,
                         Type = th.Hotel.Type,
                         Description = th.Hotel.Description,
                         Star = th.Hotel.Star,
                         AllowChild = th.Hotel.AllowChild,
                         FreeWifi = th.Hotel.FreeWifi,
                         TypeOfNutrition = th.Hotel.TypeOfNutrition,
                         Images = th.Hotel.Images
                     })

                 })
               .ToListAsync();

            if (result == null)
            {
                return NotFound("К сожалению по вашим параметрам ничего не найдено");
            }

            return Ok(result);
        }

        [HttpPost("FindByCountry")]
        public async Task<IActionResult> FindByCountry([FromBody] string country)
        {
            var result = await _tourAndHotelDbContext.Tours
                .Where(c => c.Country == country)
                .ToArrayAsync();

            return Ok(result);

        }

        [HttpPost("FindByPrice")]
        public async Task<IActionResult> SortedByPrice([FromBody] double? lowRange, double? heightRange)
        {
            var query = _tourAndHotelDbContext.Tours.AsQueryable();

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
            var hotel = await _tourAndHotelDbContext.Hotels
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

            var result = await _tourAndHotelDbContext.Tours
                .Where(x => x.Star >= rating)
                .OrderByDescending(x => x.Star)
                .ToListAsync();

            return Ok(result);
        }

        public record BookingRequest(int TourId, int Persons, string UserId);
        [HttpPatch("BookTour")]
        [Authorize]
        public async Task<IActionResult> BookATour([FromBody] BookingRequest bookingRequest)
        {

            var tour = await _tourAndHotelDbContext.Tours
                .FindAsync(bookingRequest.TourId);

    
            if (tour == null)
            {
                return NotFound("Тур не найден");
            }

            if (bookingRequest.Persons > tour.AvailableSeats)
            {
                return BadRequest("Введенное количество персон превышает количество свободных мест");
            }

            double? totalPrice = tour.Price * bookingRequest.Persons - ((tour.Price * tour.Discount) / 100);
            tour.AvailableSeats -= bookingRequest.Persons;

            _tourAndHotelDbContext.Update(tour);
            await _tourAndHotelDbContext.SaveChangesAsync();

            var userEmail = _userDbContext.Users
                .Where(x=>x.Id==bookingRequest.UserId)
                .Select(x => x.Email)
                .FirstOrDefault();

         

            var user = await _userDbContext.Users.FirstOrDefaultAsync(u => u.Id == bookingRequest.UserId);

            if (user == null)
            {
                return NotFound("Пользователь не найден");
            }

            

            _userDbContext.Update(user);
            await _userDbContext.SaveChangesAsync();


            var tourForEmail = await _tourAndHotelDbContext.Tours
            .Include(t => t) 
            .FirstOrDefaultAsync(t => t.Id == bookingRequest.TourId);

            if (tour == null)
            {
                return NotFound("Тур не найден");
            }
            var hotelIds = await _tourAndHotelDbContext.Hotels
                 .Select(h => h.Id)
                 .ToListAsync();

            var hotel = await _tourAndHotelDbContext.Hotels
                .Include(h => h.Images)
                .Where(h => hotelIds.Contains(h.Id))
                .ToListAsync();






            if (hotel != null && hotel != null)
            {
               
                var imagePath = hotel;

                var model = new 
                {
                    CustomerName = user.UserName,
                    TourName = tour.Name,
                    NumberOfPersons = bookingRequest.Persons,
                    TotalPrice = totalPrice,
                    TourDescription = tour.Description,
                    DepartureDate = tour.DepartureDate,
                    ArrivalDate = tour.ArrivalDate,
                    HotelImagePath = imagePath,
                
                };

                var emailDto = new EmailDto
                {
                    SendTo = userEmail,
                    Subject = "Бронирование тура",
                    Body = model.ToString()
                };

               
                _emailService.SendEmail(emailDto);

                
            }

            return Ok($"Тур успешно забронирован для {bookingRequest.Persons} человек. Итоговая стоимость: {totalPrice}");
        }

 

       

       





    }
}
