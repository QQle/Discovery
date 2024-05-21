using AuthServer.Api.Contextes;
using AuthServer.Api.Models;
using AuthServer.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RazorLight;
using System.Globalization;

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
                var basePrice = await GetTourPriceForHotelAsync((int)tour.Id, tour.TourHotels
                    .OrderBy(th => th.Hotel.Star)
                    .FirstOrDefault().HotelId);
                var hotelsInfo = new List<object>();

                foreach (var tourHotel in tour.TourHotels)
                {
                    double discount = 0;
                    switch (tourHotel?.Hotel?.Star)
                    {
                        case 1:
                            discount = 0.05;
                            break;
                        case 2:
                            discount = 0.04;
                            break;
                        case 3:
                            discount = 0.03;
                            break;
                        case 4:
                            discount = 0.02;
                            break;
                        case 5:
                            discount = 0.01;
                            break;
                        default:
                            break;
                    }

                    var hotelPrice = basePrice * (1 + tourHotel.Hotel.Star / 10);
                    availableSeats += tourHotel.Hotel.AvailablePerson;
                    var hotelInfo = new
                    {
                        Id = tourHotel.Hotel.Id,
                        Name = tourHotel.Hotel.Name,
                     
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
                    Price = Math.Round(basePrice, 2),
                    AvailableSeats = availableSeats,
                    Discount = tour.Discount,
                    Images = _tourAndHotelDbContext.Images.Where(x => x.TourId == tour.Id).Select(img => new
                    {
                        Url = img.Path
                    }).First(),
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
                     Images = _tourAndHotelDbContext.Images.Where(x => x.TourId == t.Id).Select(img => new
                     {
                         Url = img.Path
                     }).ToList(),
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
                         Images = _tourAndHotelDbContext.Images.Where(x => x.HotelId == th.Hotel.Id).Select(img => new
                         {
                             Url = img.Path
                         }).ToList(),
                     }).ToList()

                 })
               .ToListAsync();

            if (result == null)
            {
                return NotFound("К сожалению по вашим параметрам ничего не найдено");
            }

            return Ok(result);
        }

        public record HotelInformation(int[] HostelId);
        [HttpPost("HotelInfo")]
        public async Task<IActionResult> HotelInfo([FromBody] HotelInformation info)
        {
            var hotelInfoList = new List<Object>();

          
            var hotels = _tourAndHotelDbContext.Hotels.ToList();

            foreach (var hostelId in info.HostelId)
            {
                var hotel = hotels.FirstOrDefault(h => h.Id == hostelId);
                if (hotel != null)
                {
                    var hotelInfo = new
                    {
                        Id = hotel.Id,
                        Name = hotel.Name,
                        City = hotel.City,
                        Type = hotel.Type,
                        Description = hotel.Description,
                        Star = hotel.Star,
                        AllowChild = hotel.AllowChild,
                        FreeWifi = hotel.FreeWifi,
                        TypeOfNutrition = hotel.TypeOfNutrition,
                        AvailablePerson = hotel.AvailablePerson,
                        Images = _tourAndHotelDbContext.Images.Where(x => x.HotelId == hotel.Id).Select(img => new
                        {
                            Url = img.Path
                        }).ToList(),
                        TourHotels = hotel.TourHotels
                    };

                    hotelInfoList.Add(hotelInfo);
                }
            }

            if (!hotelInfoList.Any())
            {
                return NotFound();
            }

            return Ok(hotelInfoList);
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

        public record BookingRequest(int TourId, int Persons, string UserId, int HotelId);
        [HttpPatch("BookTour")]
        //[Authorize]
        public async Task<IActionResult> BookATour([FromBody] BookingRequest bookingRequest)
        {

            var tour = await _tourAndHotelDbContext.Tours
                      .Include(t => t.TourHotels)
                      .ThenInclude(th => th.Hotel)
                      .FirstOrDefaultAsync(t => t.Id == bookingRequest.TourId);

            if (tour == null)
            {
                return NotFound("Тур не найден");
            }

            var selectedHotel = tour?.TourHotels?.FirstOrDefault(th => th.HotelId == bookingRequest.HotelId);
            if (selectedHotel == null)
            {
                return BadRequest("Выбранный отель не доступен для данного тура");
            }

            if (bookingRequest.Persons > selectedHotel?.Hotel?.AvailablePerson)
            {
                return BadRequest("Введенное количество персон превышает количество доступных мест в отеле");
            }

            double totalPrice = await GetTourPriceForHotelAsync((int)tour.Id, selectedHotel.HotelId) * bookingRequest.Persons;

            selectedHotel.Hotel.AvailablePerson -= bookingRequest.Persons;

            _tourAndHotelDbContext.Update(tour);
            await _tourAndHotelDbContext.SaveChangesAsync();

            var user = await _userDbContext.Users
                .FirstOrDefaultAsync(u => u.Id == bookingRequest.UserId);

            var userEmail = _userDbContext.Users
                .Where(x => x.Id == bookingRequest.UserId)
                .Select(x => x.Email)
                .FirstOrDefault();

            var bookedTour = new BookedTours
            {
                UserId = bookingRequest.UserId,
                TourId = bookingRequest.TourId,
                HotelId = bookingRequest.HotelId

            };

            _tourAndHotelDbContext.BookedTours.Add(bookedTour);
            await _tourAndHotelDbContext.SaveChangesAsync();

            var tourForEmail = await _tourAndHotelDbContext.Tours
                 .FirstOrDefaultAsync(t => t.Id == bookingRequest.TourId);

            var hotelIds = await _tourAndHotelDbContext.Hotels
                 .Select(h => h.Id == bookingRequest.HotelId)
                 .ToListAsync();

            var hotel = await _tourAndHotelDbContext.Hotels
                .Include(h => h.Images)
                .ToListAsync();

            string logoImagePath = @"D:\OneDrive\Рабочий стол\Новая папка\003-RefreshToken\AuthServer.Api\Resources\Logo.svg";


            byte[] imageBytes = System.IO.File.ReadAllBytes(logoImagePath);

           
            string base64String = Convert.ToBase64String(imageBytes);

            if (hotel != null && selectedHotel != null)
            {

                var imagePath = hotel;


                var model = new
                {
                    CustomerName = user.UserName,
                    TourName = tour.Name,
                    NumberOfPersons = bookingRequest.Persons,
                    TotalPrice = totalPrice,
                    TourDescription = tour.Description,

                    TourImages = (await _tourAndHotelDbContext.Images
                                .Where(x => x.TourId == tour.Id)
                                .Select(img => img.Path)
                                .FirstOrDefaultAsync()),

                    HotelImages = (await _tourAndHotelDbContext.Images
                                .Where(x => x.HotelId == selectedHotel.HotelId)
                                .Select(img => img.Path)
                                .FirstOrDefaultAsync()),


                    HotelName = selectedHotel.Hotel.Name,
                    HotelDescription = selectedHotel.Hotel.Description,
                    LogoBase64 = "data:image/svg+xml;base64," + base64String

                };


                var emailPagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "EmailSample.cshtml");

                if (!System.IO.File.Exists(emailPagePath))
                {
                    throw new Exception("Шаблон для отправки сообщения отсутствует");
                }

                var razor = new RazorLightEngineBuilder()
                    .UseMemoryCachingProvider()
                    .Build();

                var template = await System.IO.File.ReadAllTextAsync(emailPagePath);

                var htmlContent = await razor.CompileRenderStringAsync("template", template, model);


                var emailDto = new EmailDto
                {
                    SendTo = userEmail,
                    Subject = "Бронирование тура",
                    Body = htmlContent
                };


                _emailService.SendEmail(emailDto);


            }

            return Ok($"Тур успешно забронирован для {bookingRequest.Persons} человек. Итоговая стоимость: {totalPrice}");
        }






    }
}
