using RealEstateApp.Models;
using RealEstateApp.Services;
using RealEstateApp.Views;
using System.Windows.Input;

namespace RealEstateApp.ViewModels;

[QueryProperty(nameof(PropertyListItem), "MyPropertyListItem")]
public class PropertyDetailPageViewModel : BaseViewModel, IDisposable
{
    private readonly IPropertyService service;
    private readonly ITextToSpeech textToSpeech;
    private readonly IEmail email;
    private readonly ISms sms;
    private readonly IPhoneDialer phoneDialer;
    private readonly IMap map;
    private CancellationTokenSource _speechCts;

    public PropertyDetailPageViewModel(IPropertyService service, ITextToSpeech textToSpeech, IEmail email, ISms sms, IPhoneDialer phoneDialer, IMap map)
    {
        this.service = service;
        this.textToSpeech = textToSpeech;
        this.email = email;
        this.sms = sms;
        this.phoneDialer = phoneDialer;
        this.map = map;
    }

    #region PROPERTIES
    Property property;
    public Property Property { get => property; set { SetProperty(ref property, value); } }

    Agent agent;
    public Agent Agent { get => agent; set { SetProperty(ref agent, value); } }

    PropertyListItem propertyListItem;
    public PropertyListItem PropertyListItem
    {
        set
        {
            SetProperty(ref propertyListItem, value);
           
            Property = propertyListItem.Property;
            Agent = service.GetAgents().FirstOrDefault(x => x.Id == Property.AgentId);
        }
    }

    private bool _isSpeaking;
    public bool IsSpeaking
    {
        get => _isSpeaking;
        set => SetProperty(ref _isSpeaking, value);
    }

    private float _speechPitch = 1.0f;
    public float SpeechPitch
    {
        get => _speechPitch;
        set => SetProperty(ref _speechPitch, value);
    }

    private float _speechVolume = 1.0f;
    public float SpeechVolume
    {
        get => _speechVolume;
        set => SetProperty(ref _speechVolume, value);
    }
    #endregion

    #region COMMANDS
    private Command editPropertyCommand;
    public ICommand EditPropertyCommand => editPropertyCommand ??= new Command(async () => await GotoEditProperty());

    private Command speakDescriptionCommand;
    public ICommand SpeakDescriptionCommand => speakDescriptionCommand ??= new Command(async () => await SpeakDescription());

    private Command stopSpeakingCommand;
    public ICommand StopSpeakingCommand => stopSpeakingCommand ??= new Command(async () => await StopSpeaking());

    private Command increasePitchCommand;
    public ICommand IncreasePitchCommand => increasePitchCommand ??= new Command(() => IncreasePitch());

    private Command decreasePitchCommand;
    public ICommand DecreasePitchCommand => decreasePitchCommand ??= new Command(() => DecreasePitch());

    private Command increaseVolumeCommand;
    public ICommand IncreaseVolumeCommand => increaseVolumeCommand ??= new Command(() => IncreaseVolume());

    private Command decreaseVolumeCommand;
    public ICommand DecreaseVolumeCommand => decreaseVolumeCommand ??= new Command(() => DecreaseVolume());
    
    private Command goToImageListCommand;
    public ICommand GoToImageListCommand => goToImageListCommand ??= new Command(async () => await GoToImageList());
    
    private Command callVendorCommand;
    public ICommand CallVendorCommand => callVendorCommand ??= new Command(async () => await CallVendor());
    
    private Command contactVendorCommand;
    public ICommand ContactVendorCommand => contactVendorCommand ??= new Command(async () => await ShowContactOptions());
    
    private Command emailVendorCommand;
    public ICommand EmailVendorCommand => emailVendorCommand ??= new Command(async () => await EmailVendor());

    private Command openInMapsCommand;
    public ICommand OpenInMapsCommand => openInMapsCommand ??= new Command(async () => await OpenInMaps());

    private Command openInNavigationCommand;
    public ICommand OpenInNavigationCommand => openInNavigationCommand ??= new Command(async () => await OpenInNavigation());
    #endregion

    #region TEXT-TO-SPEECH METHODS
    private async Task SpeakDescription()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Property?.Description))
            {
                await Shell.Current.DisplayAlert("No Description", "There is no description to read for this property.", "OK");
                return;
            }

            // Cancel any existing speech
            await StopSpeaking();

            // Create new cancellation token
            _speechCts = new CancellationTokenSource();
            
            // Set speaking state
            IsSpeaking = true;

            // Configure speech settings
            var speechOptions = new SpeechOptions()
            {
                Pitch = SpeechPitch,
                Volume = SpeechVolume
            };

            // Start speaking
            await textToSpeech.SpeakAsync(Property.Description, speechOptions, _speechCts.Token);
            
            // Speech completed naturally
            IsSpeaking = false;
        }
        catch (OperationCanceledException)
        {
            // Speech was cancelled - this is expected
            IsSpeaking = false;
        }
        catch (Exception ex)
        {
            IsSpeaking = false;
            await Shell.Current.DisplayAlert("Speech Error", $"Unable to speak text: {ex.Message}", "OK");
        }
    }

    private async Task StopSpeaking()
    {
        try
        {
            // Cancel ongoing speech
            _speechCts?.Cancel();
            _speechCts?.Dispose();
            _speechCts = null;

            // Stop TTS engine by calling with empty string and default options
            await textToSpeech.SpeakAsync("", new SpeechOptions(), CancellationToken.None);
            
            IsSpeaking = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error stopping speech: {ex.Message}");
            IsSpeaking = false;
        }
    }

    private void IncreasePitch()
    {
        SpeechPitch = Math.Min(2.0f, SpeechPitch + 0.1f);
    }

    private void DecreasePitch()
    {
        SpeechPitch = Math.Max(0.1f, SpeechPitch - 0.1f);
    }

    private void IncreaseVolume()
    {
        SpeechVolume = Math.Min(1.0f, SpeechVolume + 0.1f);
    }

    private void DecreaseVolume()
    {
        SpeechVolume = Math.Max(0.0f, SpeechVolume - 0.1f);
    }
    #endregion

    #region MAP METHODS
    private async Task OpenInMaps()
    {
        if (Property?.Latitude == null || Property?.Longitude == null)
        {
            await Shell.Current.DisplayAlert("No Location", "Location coordinates are not available for this property", "OK");
            return;
        }

        try
        {
            var location = new Location(Property.Latitude.Value, Property.Longitude.Value);
            var options = new MapLaunchOptions
            {
                Name = Property.Address
            };

            await map.OpenAsync(location, options);
        }
        catch (FeatureNotSupportedException)
        {
            await Shell.Current.DisplayAlert("Feature Not Supported", "Maps is not supported on this device", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Map Error", $"Unable to open maps: {ex.Message}", "OK");
        }
    }

    private async Task OpenInNavigation()
    {
        if (Property?.Latitude == null || Property?.Longitude == null)
        {
            await Shell.Current.DisplayAlert("No Location", "Location coordinates are not available for this property", "OK");
            return;
        }

        try
        {
            var location = new Location(Property.Latitude.Value, Property.Longitude.Value);
            var options = new MapLaunchOptions
            {
                Name = Property.Address,
                NavigationMode = NavigationMode.Driving
            };

            await map.OpenAsync(location, options);
        }
        catch (FeatureNotSupportedException)
        {
            await Shell.Current.DisplayAlert("Feature Not Supported", "Maps navigation is not supported on this device", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Navigation Error", $"Unable to open navigation: {ex.Message}", "OK");
        }
    }
    #endregion

    async Task GotoEditProperty()
    {
        // Stop any ongoing speech when navigating away
        await StopSpeaking();
        
        if (Property == null)
            return;

        await Shell.Current.GoToAsync($"{nameof(AddEditPropertyPage)}?mode=editproperty", true, new Dictionary<string, object>
        {
            {"MyProperty", Property }
        });
    }

    async Task GoToImageList()
    {
        // Stop any ongoing speech when navigating away
        await StopSpeaking();
        
        if (Property == null)
            return;

        try
        {
            await Shell.Current.GoToAsync(nameof(ImageListPage), true, new Dictionary<string, object>
            {
                {"Property", Property }
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Navigation Error", $"Unable to open image gallery: {ex.Message}", "OK");
        }
    }

    private async Task CallVendor()
    {
        if (Property?.Vendor?.Phone == null)
        {
            await Shell.Current.DisplayAlert("No Phone Number", "Vendor phone number is not available", "OK");
            return;
        }

        try
        {
            phoneDialer.Open(Property.Vendor.Phone);
        }
        catch (FeatureNotSupportedException)
        {
            await Shell.Current.DisplayAlert("Feature Not Supported", "Phone dialer is not supported on this device", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Call Error", $"Unable to make call: {ex.Message}", "OK");
        }
    }

    private async Task ShowContactOptions()
    {
        if (Property?.Vendor?.Phone == null)
        {
            await Shell.Current.DisplayAlert("No Phone Number", "Vendor phone number is not available", "OK");
            return;
        }

        string action = await Shell.Current.DisplayActionSheet(
            $"Contact {Property.Vendor.FullName}", 
            "Cancel", 
            null, 
            "Call", 
            "SMS");

        switch (action)
        {
            case "Call":
                await CallVendor();
                break;
            case "SMS":
                await SendSmsToVendor();
                break;
        }
    }

    private async Task SendSmsToVendor()
    {
        if (Property?.Vendor?.Phone == null)
        {
            await Shell.Current.DisplayAlert("No Phone Number", "Vendor phone number is not available", "OK");
            return;
        }

        try
        {
            var smsMessage = new SmsMessage(
                $"Hej, {Property.Vendor.FirstName}, angående {Property.Address}",
                new[] { Property.Vendor.Phone });

            await sms.ComposeAsync(smsMessage);
        }
        catch (FeatureNotSupportedException)
        {
            await Shell.Current.DisplayAlert("Feature Not Supported", "SMS is not supported on this device", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("SMS Error", $"Unable to send SMS: {ex.Message}", "OK");
        }
    }

    private async Task EmailVendor()
    {
        if (Property?.Vendor?.Email == null)
        {
            await Shell.Current.DisplayAlert("No Email Address", "Vendor email address is not available", "OK");
            return;
        }

        try
        {
            // Create property attachment file
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var attachmentFilePath = Path.Combine(folder, "property.txt");
            File.WriteAllText(attachmentFilePath, $"Property Details:\n\nAddress: {Property.Address}\nPrice: {Property.Price:C0}\nBeds: {Property.Beds}\nBaths: {Property.Baths}\nLand Size: {Property.LandSize} m²\nDescription: {Property.Description}");

            var emailMessage = new EmailMessage
            {
                To = new List<string> { Property.Vendor.Email },
                Subject = $"Inquiry about {Property.Address}",
                Body = $"Dear {Property.Vendor.FirstName},\n\nI am interested in learning more about the property at {Property.Address}.\n\nPlease find the property details attached.\n\nBest regards",
                Attachments = new List<EmailAttachment>
                {
                    new EmailAttachment(attachmentFilePath)
                }
            };

            await email.ComposeAsync(emailMessage);
        }
        catch (FeatureNotSupportedException)
        {
            await Shell.Current.DisplayAlert("Feature Not Supported", "Email is not supported on this device", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Email Error", $"Unable to send email: {ex.Message}", "OK");
        }
    }

    public void Dispose()
    {
        // Clean up speech when disposing
        _speechCts?.Cancel();
        _speechCts?.Dispose();
    }
}
