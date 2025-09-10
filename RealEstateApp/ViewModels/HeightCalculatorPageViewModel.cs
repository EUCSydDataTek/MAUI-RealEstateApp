using RealEstateApp.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RealEstateApp.ViewModels;

public class HeightCalculatorPageViewModel : BaseViewModel, IDisposable
{
    private readonly IBarometer barometer;
    private readonly IGeolocation geolocation;
    private bool _isBarometerActive = false;
    
    // Sea level pressure for Denmark (typical value from DMI)
    private const double SEA_LEVEL_PRESSURE = 1013.25; // hPa

    public HeightCalculatorPageViewModel(IBarometer barometer, IGeolocation geolocation)
    {
        this.barometer = barometer;
        this.geolocation = geolocation;
        Title = "Height Calculator";
        Measurements = new ObservableCollection<BarometerMeasurement>();
    }

    #region Properties
    private double _currentPressure;
    public double CurrentPressure
    {
        get => _currentPressure;
        set 
        { 
            SetProperty(ref _currentPressure, value);
            CalculateAltitude();
        }
    }

    private double _currentAltitude;
    public double CurrentAltitude
    {
        get => _currentAltitude;
        set => SetProperty(ref _currentAltitude, value);
    }

    private string _measurementLabel;
    public string MeasurementLabel
    {
        get => _measurementLabel;
        set => SetProperty(ref _measurementLabel, value);
    }

    public ObservableCollection<BarometerMeasurement> Measurements { get; }

    private string _statusMessage;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    #endregion

    #region Commands
    private Command saveMeasurementCommand;
    public ICommand SaveMeasurementCommand => saveMeasurementCommand ??= new Command(SaveMeasurement);

    private Command calibrateCommand;
    public ICommand CalibrateCommand => calibrateCommand ??= new Command(async () => await CalibrateWithGPS());
    #endregion

    public async Task OnAppearing()
    {
        await StartBarometer();
    }

    private async Task StartBarometer()
    {
        try
        {
            if (!barometer.IsSupported)
            {
                StatusMessage = "Barometer not supported on this device";
                await Shell.Current.DisplayAlert("Barometer Error", "Barometer is not supported on this device", "OK");
                return;
            }

            if (!_isBarometerActive)
            {
                barometer.ReadingChanged += OnBarometerReadingChanged;
                barometer.Start(SensorSpeed.UI);
                _isBarometerActive = true;
                StatusMessage = "Barometer active";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error starting barometer: {ex.Message}";
            await Shell.Current.DisplayAlert("Barometer Error", $"Unable to start barometer: {ex.Message}", "OK");
        }
    }

    public async Task StopBarometer()
    {
        try
        {
            if (_isBarometerActive)
            {
                barometer.Stop();
                barometer.ReadingChanged -= OnBarometerReadingChanged;
                _isBarometerActive = false;
                StatusMessage = "Barometer stopped";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error stopping barometer: {ex.Message}");
        }
    }

    private void OnBarometerReadingChanged(object sender, BarometerChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentPressure = e.Reading.PressureInHectopascals;
        });
    }

    private void CalculateAltitude()
    {
        // Formula: AltitudeInMeters = 44307.694 * (1 - Math.Pow(currentPressure / seaLevelPressure, 0.190284))
        if (CurrentPressure > 0)
        {
            CurrentAltitude = 44307.694 * (1 - Math.Pow(CurrentPressure / SEA_LEVEL_PRESSURE, 0.190284));
        }
    }

    private void SaveMeasurement()
    {
        if (string.IsNullOrWhiteSpace(MeasurementLabel))
        {
            StatusMessage = "Please enter a measurement label";
            return;
        }

        var newMeasurement = new BarometerMeasurement
        {
            Pressure = CurrentPressure,
            Altitude = CurrentAltitude,
            Label = MeasurementLabel,
            HeightChange = 0
        };

        // Calculate height change from previous measurement
        if (Measurements.Count > 0)
        {
            var lastMeasurement = Measurements.Last();
            newMeasurement.HeightChange = CurrentAltitude - lastMeasurement.Altitude;
        }

        Measurements.Add(newMeasurement);
        
        StatusMessage = $"Measurement saved: {MeasurementLabel}";
        MeasurementLabel = string.Empty; // Clear the input field
    }

    private async Task CalibrateWithGPS()
    {
        try
        {
            StatusMessage = "Getting GPS altitude...";
            
            var request = new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Best,
                Timeout = TimeSpan.FromSeconds(15)
            };

            var location = await geolocation.GetLocationAsync(request);
            
            if (location?.Altitude.HasValue == true)
            {
                // Use GPS altitude to calibrate barometer
                double gpsAltitude = location.Altitude.Value;
                
                // Reverse calculate what sea level pressure should be
                // altitude = 44307.694 * (1 - (pressure / seaLevel)^0.190284)
                // Solving for seaLevel: seaLevel = pressure / (1 - altitude/44307.694)^(1/0.190284)
                
                if (CurrentPressure > 0)
                {
                    double calculatedSeaLevel = CurrentPressure / Math.Pow(1 - gpsAltitude / 44307.694, 1 / 0.190284);
                    
                    await Shell.Current.DisplayAlert("GPS Calibration", 
                        $"GPS Altitude: {gpsAltitude:N2}m\nCurrent Pressure: {CurrentPressure:N2} hPa\nCalculated Sea Level: {calculatedSeaLevel:N2} hPa\n\nUse this for more accurate readings!", 
                        "OK");
                        
                    StatusMessage = $"GPS calibration complete - Altitude: {gpsAltitude:N2}m";
                }
            }
            else
            {
                StatusMessage = "GPS altitude not available";
                await Shell.Current.DisplayAlert("GPS Error", "Unable to get GPS altitude", "OK");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"GPS calibration failed: {ex.Message}";
            await Shell.Current.DisplayAlert("GPS Error", $"Calibration failed: {ex.Message}", "OK");
        }
    }

    public void Dispose()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await StopBarometer();
        });
    }
}