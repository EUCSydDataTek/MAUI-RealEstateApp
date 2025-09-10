using RealEstateApp.Models;
using System.Windows.Input;

namespace RealEstateApp.ViewModels;

[QueryProperty(nameof(Property), "Property")]
public class CompassPageViewModel : BaseViewModel, IDisposable
{
    private readonly IMagnetometer magnetometer;
    private bool _isCompassActive = false;
    private const double SMOOTHING_FACTOR = 0.8; // Damping factor

    public CompassPageViewModel(IMagnetometer magnetometer)
    {
        this.magnetometer = magnetometer;
        Title = "Compass";
    }

    #region Properties
    private Property _property;
    public Property Property 
    { 
        get => _property;
        set 
        {
            SetProperty(ref _property, value);
        }
    }

    private double _currentHeading;
    public double CurrentHeading
    {
        get => _currentHeading;
        set => SetProperty(ref _currentHeading, value);
    }

    private double _rotationAngle;
    public double RotationAngle
    {
        get => _rotationAngle;
        set => SetProperty(ref _rotationAngle, value);
    }

    private string _currentAspect = "North";
    public string CurrentAspect
    {
        get => _currentAspect;
        set => SetProperty(ref _currentAspect, value);
    }
    #endregion

    #region Commands
    private Command setAspectCommand;
    public ICommand SetAspectCommand => setAspectCommand ??= new Command(async () => await SetAspectAndGoBack());
    #endregion

    public async Task OnAppearing()
    {
        await StartCompass();
    }

    private async Task SetAspectAndGoBack()
    {
        if (Property != null)
        {
            Property.Aspect = CurrentAspect;
        }
        
        await StopCompass();
        await Shell.Current.GoToAsync("..");
    }

    public async Task StartCompass()
    {
        try
        {
            if (!magnetometer.IsSupported)
            {
                await Shell.Current.DisplayAlert("Compass Error", "Magnetometer is not supported on this device", "OK");
                return;
            }

            if (!_isCompassActive)
            {
                magnetometer.ReadingChanged += OnMagnetometerReadingChanged;
                magnetometer.Start(SensorSpeed.UI);
                _isCompassActive = true;
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Compass Error", $"Unable to start compass: {ex.Message}", "OK");
        }
    }

    public async Task StopCompass()
    {
        try
        {
            if (_isCompassActive)
            {
                magnetometer.Stop();
                magnetometer.ReadingChanged -= OnMagnetometerReadingChanged;
                _isCompassActive = false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error stopping compass: {ex.Message}");
        }
    }

    private void OnMagnetometerReadingChanged(object sender, MagnetometerChangedEventArgs e)
    {
        var reading = e.Reading;
        
        // Calculate heading from magnetometer reading
        double heading = Math.Atan2(reading.MagneticField.Y, reading.MagneticField.X) * (180 / Math.PI);
        
        // Normalize to 0-360 degrees
        if (heading < 0)
            heading += 360;

        // Apply smoothing/damping
        CurrentHeading = SMOOTHING_FACTOR * CurrentHeading + (1 - SMOOTHING_FACTOR) * heading;

        // Rotation angle is opposite to heading (compass needle points north while device rotates)
        RotationAngle = -CurrentHeading;

        // Calculate aspect based on heading
        CurrentAspect = CalculateAspect(CurrentHeading);
    }

    private string CalculateAspect(double heading)
    {
        // Round to nearest cardinal direction
        // North: 315-360/0-45, East: 45-135, South: 135-225, West: 225-315
        
        if (heading >= 315 || heading < 45)
            return "North";
        else if (heading >= 45 && heading < 135)
            return "East";
        else if (heading >= 135 && heading < 225)
            return "South";
        else if (heading >= 225 && heading < 315)
            return "West";
        
        return "North"; // Default fallback
    }

    public void Dispose()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await StopCompass();
        });
    }
}