using RealEstateApp.Models;
using RealEstateApp.Services;
using RealEstateApp.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace RealEstateApp.ViewModels;

public class PropertyListPageViewModel : BaseViewModel
{
    public ObservableCollection<PropertyListItem> PropertiesCollection { get; } = new();

    private readonly IPropertyService service;
    private Location _currentLocation;

    public PropertyListPageViewModel(IPropertyService service)
    {
        Title = "Property List";
        this.service = service;
    }

    bool isRefreshing;
    public bool IsRefreshing
    {
        get => isRefreshing;
        set => SetProperty(ref isRefreshing, value);
    }

    #region COMMANDS
    private Command getPropertiesCommand;
    public ICommand GetPropertiesCommand => getPropertiesCommand ??= new Command(async () => await GetPropertiesAsync());

    private Command refreshCommand;
    public ICommand RefreshCommand => refreshCommand ??= new Command(async () => await GetPropertiesAsync());

    private Command<PropertyListItem> goToDetailsCommand;
    public ICommand GoToDetailsCommand => goToDetailsCommand ??= new Command<PropertyListItem>(async (item) => await GoToDetails(item));

    private Command goToAddPropertyCommand;
    public ICommand GoToAddPropertyCommand => goToAddPropertyCommand ??= new Command(async () => await GotoAddProperty());

    private Command sortCommand;
    public ICommand SortCommand => sortCommand ??= new Command(async () => await SortAsync());
    #endregion

    async Task GetPropertiesAsync()
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;

            List<Property> properties = service.GetProperties();

            if (PropertiesCollection.Count != 0)
                PropertiesCollection.Clear();

            // Create PropertyListItem collection
            var propertyListItems = new List<PropertyListItem>();
            
            foreach (Property property in properties)
            {
                var propertyListItem = new PropertyListItem(property);
                
                // Calculate distance if we have current location and property has coordinates
                if (_currentLocation != null && 
                    property.Latitude.HasValue && 
                    property.Longitude.HasValue)
                {
                    var propertyLocation = new Location(property.Latitude.Value, property.Longitude.Value);
                    double distanceInMeters = Location.CalculateDistance(_currentLocation, propertyLocation, DistanceUnits.Kilometers);
                    propertyListItem.Distance = distanceInMeters;
                }
                
                propertyListItems.Add(propertyListItem);
            }

            // Sort by distance if we have location data
            if (_currentLocation != null)
            {
                propertyListItems = propertyListItems
                    .OrderBy(x => x.Distance)
                    .ToList();
            }

            // Add sorted items to the observable collection
            foreach (var item in propertyListItems)
            {
                PropertiesCollection.Add(item);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to get properties: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    private async Task SortAsync()
    {
        try
        {
            // Get last known location first
            _currentLocation = await Geolocation.GetLastKnownLocationAsync();
            
            // If no last known location, get current location
            if (_currentLocation == null)
            {
                var request = new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(10)
                };

                var cts = new CancellationTokenSource();
                _currentLocation = await Geolocation.GetLocationAsync(request, cts.Token);
            }

            if (_currentLocation != null)
            {
                // Refresh the properties list with distance calculations
                await GetPropertiesAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert("Location Error", "Unable to get current location", "OK");
            }
        }
        catch (FeatureNotSupportedException)
        {
            await Shell.Current.DisplayAlert("Error", "Geolocation is not supported on this device", "OK");
        }
        catch (FeatureNotEnabledException)
        {
            await Shell.Current.DisplayAlert("Error", "Geolocation is not enabled", "OK");
        }
        catch (PermissionException)
        {
            await Shell.Current.DisplayAlert("Error", "Location permission denied", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", "Unable to get location", "OK");
            Debug.WriteLine($"Location error: {ex.Message}");
        }
    }

    async Task GoToDetails(PropertyListItem propertyListItem)
    {
        if (propertyListItem == null)
            return;

        await Shell.Current.GoToAsync(nameof(PropertyDetailPage), true, new Dictionary<string, object>
        {
            {"MyPropertyListItem", propertyListItem }
        });
    }

    async Task GotoAddProperty()
    {
        await Shell.Current.GoToAsync($"{nameof(AddEditPropertyPage)}?mode=newproperty", true, new Dictionary<string, object>
        {
            {"MyProperty", new Property() }
        });
    }
}
