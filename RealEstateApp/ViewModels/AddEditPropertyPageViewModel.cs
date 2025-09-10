using RealEstateApp.Models;
using RealEstateApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RealEstateApp.ViewModels;

[QueryProperty(nameof(Mode), "mode")]
[QueryProperty(nameof(Property), "MyProperty")]
public class AddEditPropertyPageViewModel : BaseViewModel, IDisposable
{
    readonly IPropertyService service;
    readonly IConnectivity connectivity;
    readonly IVibration vibration;
    readonly IHapticFeedback hapticFeedback;

    // Cancellation token for vibration
    private CancellationTokenSource _vibrationCts;

    public AddEditPropertyPageViewModel(IPropertyService service, IConnectivity connectivity, IVibration vibration, IHapticFeedback hapticFeedback)
    {
        this.service = service;
        this.connectivity = connectivity;
        this.vibration = vibration;
        this.hapticFeedback = hapticFeedback;
        Agents = new ObservableCollection<Agent>(service.GetAgents());
        
        // Subscribe to connectivity changes
        this.connectivity.ConnectivityChanged += OnConnectivityChanged;
        
        // Initialize network status
        UpdateNetworkStatus();
    }

    public string Mode { get; set; }

    #region PROPERTIES
    public ObservableCollection<Agent> Agents { get; }

    private Property _property;
    public Property Property
    {
        get => _property;
        set
        {
            SetProperty(ref _property, value);
            Title = Mode == "newproperty" ? "Add Property" : "Edit Property";

            if (_property.AgentId != null)
            {
                SelectedAgent = Agents.FirstOrDefault(x => x.Id == _property?.AgentId);
            }
        }
    }

    private Agent _selectedAgent;
    public Agent SelectedAgent
    {
        get => _selectedAgent;
        set
        {
            if (Property != null)
            {
                _selectedAgent = value;
                Property.AgentId = _selectedAgent?.Id;
            }
        }
    }

    string statusMessage;
    public string StatusMessage
    {
        get { return statusMessage; }
        set { SetProperty(ref statusMessage, value); }
    }

    Color statusColor;
    public Color StatusColor
    {
        get { return statusColor; }
        set { SetProperty(ref statusColor, value); }
    }

    private bool _isNetworkAvailable;
    public bool IsNetworkAvailable
    {
        get => _isNetworkAvailable;
        set => SetProperty(ref _isNetworkAvailable, value);
    }

    private bool _hasShownInitialConnectivityAlert = false;
    #endregion

    #region COMMANDS
    private Command savePropertyCommand;
    public ICommand SavePropertyCommand => savePropertyCommand ??= new Command(async () => await SaveProperty());
    
    private Command cancelSaveCommand;
    public ICommand CancelSaveCommand => cancelSaveCommand ??= new Command(async () => await CancelSave());
    
    private Command getLocationCommand;
    public ICommand GetLocationCommand => getLocationCommand ??= new Command(async () => await GetCurrentLocation());
    
    private Command geocodeAddressCommand;
    public ICommand GeocodeAddressCommand => geocodeAddressCommand ??= new Command(async () => await GeocodeAddress());
    #endregion

    #region HAPTIC AND VIBRATION METHODS
    private void TriggerErrorFeedback()
    {
        try
        {
            // Haptic feedback for error
            hapticFeedback.Perform(HapticFeedbackType.LongPress);
            
            // Start 5-second vibration
            StartErrorVibration();
        }
        catch (Exception ex)
        {
            // Vibration not supported on this device
            System.Diagnostics.Debug.WriteLine($"Vibration/Haptic not supported: {ex.Message}");
        }
    }

    private void TriggerSuccessFeedback()
    {
        try
        {
            // Haptic feedback for success
            hapticFeedback.Perform(HapticFeedbackType.Click);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Haptic feedback not supported: {ex.Message}");
        }
    }

    private void TriggerLocationFeedback()
    {
        try
        {
            // Haptic feedback for location actions
            hapticFeedback.Perform(HapticFeedbackType.LongPress);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Haptic feedback not supported: {ex.Message}");
        }
    }

    private void StartErrorVibration()
    {
        try
        {
            // Cancel any existing vibration
            CancelVibration();
            
            // Create new cancellation token
            _vibrationCts = new CancellationTokenSource();
            
            // Start 5-second vibration
            vibration.Vibrate(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Vibration not supported: {ex.Message}");
        }
    }

    private void CancelVibration()
    {
        try
        {
            // Cancel vibration
            vibration.Cancel();
            
            // Cancel any existing cancellation token
            _vibrationCts?.Cancel();
            _vibrationCts?.Dispose();
            _vibrationCts = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cancelling vibration: {ex.Message}");
        }
    }
    #endregion

    #region CONNECTIVITY METHODS
    private void UpdateNetworkStatus()
    {
        IsNetworkAvailable = connectivity.NetworkAccess == NetworkAccess.Internet;
        
        // Show initial connectivity alert when page loads
        if (!_hasShownInitialConnectivityAlert)
        {
            _hasShownInitialConnectivityAlert = true;
            if (!IsNetworkAvailable)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.DisplayAlert("No Internet Connection", 
                        "Internet connection is required for geocoding features. Please check your connection.", "OK");
                });
            }
        }
    }

    private void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
    {
        bool wasConnected = IsNetworkAvailable;
        UpdateNetworkStatus();
        
        // Show alert when connectivity changes (but not on initial load)
        if (_hasShownInitialConnectivityAlert)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (IsNetworkAvailable && !wasConnected)
                {
                    await Shell.Current.DisplayAlert("Internet Connected", 
                        "Internet connection restored. Geocoding features are now available.", "OK");
                    StatusMessage = "Internet connection restored";
                    StatusColor = Colors.Green;
                    TriggerSuccessFeedback();
                }
                else if (!IsNetworkAvailable && wasConnected)
                {
                    await Shell.Current.DisplayAlert("No Internet Connection", 
                        "Internet connection lost. Geocoding features are unavailable.", "OK");
                    StatusMessage = "No internet connection";
                    StatusColor = Colors.Red;
                    TriggerErrorFeedback();
                }
            });
        }
    }
    #endregion

    private async Task CancelSave()
    {
        // Cancel any ongoing vibration when canceling
        CancelVibration();
        
        // Haptic feedback for cancel action
        TriggerSuccessFeedback();
        
        await Shell.Current.GoToAsync("..");
    }

    private async Task SaveProperty()
    {
        if (IsValid() == false)
        {
            StatusMessage = "Please fill in all required fields";
            StatusColor = Colors.Red;
            
            // Trigger error feedback for invalid data
            TriggerErrorFeedback();
        }
        else
        {
            // Cancel any vibration before saving
            CancelVibration();
            
            service.SaveProperty(Property);
            StatusMessage = "Property saved successfully";
            StatusColor = Colors.Green;
            
            // Trigger success feedback
            TriggerSuccessFeedback();
            
            await Shell.Current.GoToAsync("///propertylist");
        }
    }

    public bool IsValid()
    {
        if (string.IsNullOrEmpty(Property.Address)
            || Property.Beds == null
            || Property.Price == null
            || Property.AgentId == null)
            return false;
        return true;
    }

    private async Task GetCurrentLocation()
    {
        // Haptic feedback for location action
        TriggerLocationFeedback();
        
        try
        {
            var request = new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Medium,
                Timeout = TimeSpan.FromSeconds(10)
            };

            var cts = new CancellationTokenSource();
            var location = await Geolocation.GetLocationAsync(request, cts.Token);

            if (location != null)
            {
                Property.Latitude = location.Latitude;
                Property.Longitude = location.Longitude;
                
                // Only perform reverse geocoding if network is available
                if (IsNetworkAvailable)
                {
                    await ReverseGeocodeLocation(location);
                    StatusMessage = "Location and address updated successfully";
                }
                else
                {
                    StatusMessage = "Location updated (address unavailable - no internet)";
                }
                
                // Refresh UI binding
                OnPropertyChanged(nameof(Property));
                StatusColor = Colors.Green;
                
                // Success feedback
                TriggerSuccessFeedback();
            }
        }
        catch (FeatureNotSupportedException)
        {
            StatusMessage = "Geolocation is not supported on this device";
            StatusColor = Colors.Red;
            TriggerErrorFeedback();
        }
        catch (FeatureNotEnabledException)
        {
            StatusMessage = "Geolocation is not enabled";
            StatusColor = Colors.Red;
            TriggerErrorFeedback();
        }
        catch (PermissionException)
        {
            StatusMessage = "Location permission denied";
            StatusColor = Colors.Red;
            TriggerErrorFeedback();
        }
        catch (Exception ex)
        {
            StatusMessage = "Unable to get location";
            StatusColor = Colors.Red;
            TriggerErrorFeedback();
        }
    }

    private async Task ReverseGeocodeLocation(Location location)
    {
        try
        {
            var placemarks = await Geocoding.GetPlacemarksAsync(location.Latitude, location.Longitude);
            var placemark = placemarks?.FirstOrDefault();

            if (placemark != null)
            {
                // Build address string from placemark
                var addressParts = new List<string>();
                
                if (!string.IsNullOrEmpty(placemark.SubThoroughfare))
                    addressParts.Add(placemark.SubThoroughfare);
                
                if (!string.IsNullOrEmpty(placemark.Thoroughfare))
                    addressParts.Add(placemark.Thoroughfare);
                    
                if (!string.IsNullOrEmpty(placemark.Locality))
                    addressParts.Add(placemark.Locality);
                    
                if (!string.IsNullOrEmpty(placemark.PostalCode))
                    addressParts.Add(placemark.PostalCode);
                    
                if (!string.IsNullOrEmpty(placemark.CountryName))
                    addressParts.Add(placemark.CountryName);

                Property.Address = string.Join(", ", addressParts);
            }
        }
        catch (Exception ex)
        {
            // Reverse geocoding failed, but that's okay - we still have coordinates
            StatusMessage = "Location updated, but unable to get address";
            StatusColor = Colors.Orange;
        }
    }

    private async Task GeocodeAddress()
    {
        // Haptic feedback for geocoding action
        TriggerLocationFeedback();
        
        try
        {
            // Check network connectivity first
            if (!IsNetworkAvailable)
            {
                await Shell.Current.DisplayAlert("No Internet Connection", 
                    "Internet connection is required for geocoding. Please check your connection.", "OK");
                TriggerErrorFeedback();
                return;
            }

            // Check if address field is empty
            if (string.IsNullOrWhiteSpace(Property?.Address))
            {
                await Shell.Current.DisplayAlert("Address Required", "Please enter an address first", "OK");
                TriggerErrorFeedback();
                return;
            }

            // Perform geocoding
            var locations = await Geocoding.GetLocationsAsync(Property.Address);
            var location = locations?.FirstOrDefault();

            if (location != null)
            {
                Property.Latitude = location.Latitude;
                Property.Longitude = location.Longitude;
                
                // Refresh UI binding
                OnPropertyChanged(nameof(Property));
                
                StatusMessage = "Coordinates updated successfully";
                StatusColor = Colors.Green;
                TriggerSuccessFeedback();
            }
            else
            {
                StatusMessage = "Unable to find coordinates for this address";
                StatusColor = Colors.Red;
                TriggerErrorFeedback();
            }
        }
        catch (FeatureNotSupportedException)
        {
            StatusMessage = "Geocoding is not supported on this device";
            StatusColor = Colors.Red;
            TriggerErrorFeedback();
        }
        catch (Exception ex)
        {
            StatusMessage = "Unable to geocode address - check your internet connection";
            StatusColor = Colors.Red;
            TriggerErrorFeedback();
        }
    }

    public void Dispose()
    {
        // Cancel any ongoing vibration
        CancelVibration();
        
        // Unsubscribe from connectivity changes to prevent memory leaks
        connectivity.ConnectivityChanged -= OnConnectivityChanged;
    }
}
