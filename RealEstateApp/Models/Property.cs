using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RealEstateApp.Models
{
    public class Property : INotifyPropertyChanged
    {
        public Property()
        {
            Id = Guid.NewGuid().ToString();
            ImageUrls = new List<string>();
        }

        public string Id { get; set; }
        public string Address { get; set; }
        public int? Price { get; set; }
        public string Description { get; set; }
        public int? Beds { get; set; }
        public int? Baths { get; set; }
        public int? Parking { get; set; }
        public int? LandSize { get; set; }
        public string AgentId { get; set; }
        public List<string> ImageUrls { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string NeighbourhoodUrl { get; set; }
        public Vendor Vendor { get; set; }

        private string _aspect;
        public string Aspect
        {
            get => _aspect;
            set
            {
                _aspect = value;
                OnPropertyChanged();
            }
        }

        public string MainImageUrl => ImageUrls?.FirstOrDefault() ?? GlobalSettings.Instance.NoImageUrl;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
