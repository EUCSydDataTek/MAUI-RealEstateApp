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
    private CancellationTokenSource _speechCts;

    public PropertyDetailPageViewModel(IPropertyService service, ITextToSpeech textToSpeech)
    {
        this.service = service;
        this.textToSpeech = textToSpeech;
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

    public void Dispose()
    {
        // Clean up speech when disposing
        _speechCts?.Cancel();
        _speechCts?.Dispose();
    }
}
