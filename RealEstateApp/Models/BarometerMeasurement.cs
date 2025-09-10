namespace RealEstateApp.Models
{
    public class BarometerMeasurement
    {
        public double Pressure { get; set; }
        public double Altitude { get; set; }
        public string Label { get; set; }
        public double HeightChange { get; set; }

        public string Display => $"{Label}: {Altitude:N2}m";
    }
}