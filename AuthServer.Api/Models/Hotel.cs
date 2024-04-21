namespace AuthServer.Api.Models
{
    public class Hotel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public double Star {  get; set; }
        public bool AllowChild { get; set; }
        public bool FreeWifi { get; set; }
        public string TypeOfNutrition { get; set; }
        public Images Images { get; set; }




    }
}
