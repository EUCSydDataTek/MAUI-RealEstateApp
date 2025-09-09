using RealEstateApp.Models;
using RealEstateApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RealEstateApp.ViewModels;

[QueryProperty(nameof(Mode), "mode")]
[QueryProperty(nameof(Property), "MyProperty")]
public class AddEditPropertyPageViewModel : BaseViewModel
{
    readonly IPropertyService service;

    public AddEditPropertyPageViewModel(IPropertyService service)
    {
        this.service = service;
        Agents = new ObservableCollection<Agent>(service.GetAgents());
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
    #endregion

    #region COMMANDS
    private Command savePropertyCommand;
    public ICommand SavePropertyCommand => savePropertyCommand ??= new Command(async () => await SaveProperty());
    
    private Command cancelSaveCommand;
    public ICommand CancelSaveCommand => cancelSaveCommand ??= new Command(async () => await Shell.Current.GoToAsync(".."));
    
    private Command getLocationCommand;
    public ICommand GetLocationCommand => getLocationCommand ??= new Command(async () => await GetCurrentLocation());
    #endregion

    private async Task SaveProperty()
    {
        if (IsValid() == false)
        {
           StatusMessage = "Please fill in all required fields";
            StatusColor = Colors.Red;
        }
        else
        {
            service.SaveProperty(Property);
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
                
                // Refresh UI binding
                OnPropertyChanged(nameof(Property));
                
                StatusMessage = "Location updated successfully";
                StatusColor = Colors.Green;
            }
        }
        catch (FeatureNotSupportedException fnsEx)
        {
            StatusMessage = "Geolocation is not supported on this device";
            StatusColor = Colors.Red;
        }
        catch (FeatureNotEnabledException fneEx)
        {
            StatusMessage = "Geolocation is not enabled";
            StatusColor = Colors.Red;
        }
        catch (PermissionException pEx)
        {
            StatusMessage = "Location permission denied";
            StatusColor = Colors.Red;
        }
        catch (Exception ex)
        {
            StatusMessage = "Unable to get location";
            StatusColor = Colors.Red;
        }
    }
}
