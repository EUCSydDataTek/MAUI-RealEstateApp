using RealEstateApp.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RealEstateApp.ViewModels;

[QueryProperty(nameof(Property), "Property")]
public class ImageListPageViewModel : BaseViewModel, IDisposable
{
    private readonly IAccelerometer accelerometer;
    private bool _isShakeActive = false;
    private DateTime _lastShakeTime = DateTime.MinValue;
    private const double SHAKE_THRESHOLD = 15.0; // Sensitivity for shake detection
    private const int SHAKE_COOLDOWN_MS = 1000; // Prevent rapid fire shakes

    public ImageListPageViewModel(IAccelerometer accelerometer)
    {
        this.accelerometer = accelerometer;
        Images = new ObservableCollection<string>();
    }

    #region Properties
    private Property _property;
    public Property Property 
    { 
        get => _property;
        set 
        {
            SetProperty(ref _property, value);
            Title = "Property Images";
            LoadImages();
        }
    }

    public ObservableCollection<string> Images { get; }

    private int _position;
    public int Position
    {
        get => _position;
        set 
        {
            // Ensure position stays within bounds (0-2)
            if (value < 0)
                _position = Images.Count - 1; // Wrap to last image
            else if (value >= Images.Count)
                _position = 0; // Wrap to first image
            else
                _position = value;
                
            SetProperty(ref _position, _position);
            UpdateCurrentImageDescription();
        }
    }

    private string _currentImageDescription;
    public string CurrentImageDescription
    {
        get => _currentImageDescription;
        set => SetProperty(ref _currentImageDescription, value);
    }

    private string _shakeStatusMessage;
    public string ShakeStatusMessage
    {
        get => _shakeStatusMessage;
        set => SetProperty(ref _shakeStatusMessage, value);
    }

    // Accelerometer properties for extra task
    private double _accelerometerX;
    public double AccelerometerX
    {
        get => _accelerometerX;
        set => SetProperty(ref _accelerometerX, value);
    }

    private double _accelerometerY;
    public double AccelerometerY
    {
        get => _accelerometerY;
        set => SetProperty(ref _accelerometerY, value);
    }

    private double _accelerometerZ;
    public double AccelerometerZ
    {
        get => _accelerometerZ;
        set => SetProperty(ref _accelerometerZ, value);
    }
    #endregion

    public async Task OnAppearing()
    {
        await StartShakeDetection();
    }

    public async Task OnDisappearing()
    {
        await StopShakeDetection();
    }

    private void LoadImages()
    {
        Images.Clear();
        
        if (Property?.ImageUrls != null && Property.ImageUrls.Count > 0)
        {
            foreach (var imageUrl in Property.ImageUrls)
            {
                Images.Add(imageUrl);
            }
        }
        
        Position = 0; // Start with first image
        UpdateCurrentImageDescription();
    }

    private void UpdateCurrentImageDescription()
    {
        if (Images.Count == 0) return;

        string description = Position switch
        {
            0 => "Exterior View",
            1 => "Kitchen",
            2 => "Bedroom",
            _ => $"Image {Position + 1}"
        };

        CurrentImageDescription = $"{description} ({Position + 1} of {Images.Count})";
    }

    private async Task StartShakeDetection()
    {
        try
        {
            if (!accelerometer.IsSupported)
            {
                ShakeStatusMessage = "Shake detection not supported";
                return;
            }

            if (!_isShakeActive)
            {
                accelerometer.ReadingChanged += OnAccelerometerReadingChanged;
                accelerometer.Start(SensorSpeed.Game); // Higher frequency for shake detection
                _isShakeActive = true;
                ShakeStatusMessage = "Shake to change images!";
            }
        }
        catch (Exception ex)
        {
            ShakeStatusMessage = $"Shake detection error: {ex.Message}";
        }
    }

    private async Task StopShakeDetection()
    {
        try
        {
            if (_isShakeActive)
            {
                accelerometer.Stop();
                accelerometer.ReadingChanged -= OnAccelerometerReadingChanged;
                _isShakeActive = false;
                ShakeStatusMessage = "";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error stopping shake detection: {ex.Message}");
        }
    }

    private void OnAccelerometerReadingChanged(object sender, AccelerometerChangedEventArgs e)
    {
        var reading = e.Reading;
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Update accelerometer values for display (extra task)
            AccelerometerX = reading.Acceleration.X;
            AccelerometerY = reading.Acceleration.Y;
            AccelerometerZ = reading.Acceleration.Z;
            
            // Calculate total acceleration magnitude
            double totalAcceleration = Math.Sqrt(
                Math.Pow(reading.Acceleration.X, 2) + 
                Math.Pow(reading.Acceleration.Y, 2) + 
                Math.Pow(reading.Acceleration.Z, 2)
            );

            // Detect shake (sudden high acceleration)
            if (totalAcceleration > SHAKE_THRESHOLD)
            {
                DetectShake(reading.Acceleration.X);
            }
        });
    }

    private void DetectShake(double xAcceleration)
    {
        // Prevent rapid fire shakes
        if ((DateTime.Now - _lastShakeTime).TotalMilliseconds < SHAKE_COOLDOWN_MS)
            return;

        _lastShakeTime = DateTime.Now;

        // Determine shake direction based on X acceleration
        if (Math.Abs(xAcceleration) > 5.0) // Significant horizontal movement
        {
            if (xAcceleration > 0)
            {
                // Shake right - next image
                Position = Position + 1;
                ShakeStatusMessage = "Shook right - Next image!";
            }
            else
            {
                // Shake left - previous image  
                Position = Position - 1;
                ShakeStatusMessage = "Shook left - Previous image!";
            }
        }
        else
        {
            // General shake - just go to next image
            Position = Position + 1;
            ShakeStatusMessage = "Shake detected - Next image!";
        }

        // Clear message after delay
        Device.StartTimer(TimeSpan.FromSeconds(2), () =>
        {
            ShakeStatusMessage = "Shake to change images!";
            return false; // Don't repeat
        });
    }

    public void Dispose()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await StopShakeDetection();
        });
    }
}