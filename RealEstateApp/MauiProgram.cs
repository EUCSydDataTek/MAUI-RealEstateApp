using Microsoft.Extensions.Logging;
using RealEstateApp.Repositories;
using RealEstateApp.Services;
using RealEstateApp.ViewModels;
using RealEstateApp.Views;

namespace RealEstateApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("fa-solid-900.ttf", "FA-solid");
            });

        // Register Connectivity service
        builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
        
        // Register Vibration and HapticFeedback services
        builder.Services.AddSingleton<IVibration>(Vibration.Default);
        builder.Services.AddSingleton<IHapticFeedback>(HapticFeedback.Default);

        builder.Services.AddSingleton<IPropertyService, MockRepository>();
        builder.Services.AddSingleton<PropertyListPage>();
        builder.Services.AddSingleton<PropertyListPageViewModel>();

        builder.Services.AddTransient<PropertyDetailPage>();
        builder.Services.AddTransient<PropertyDetailPageViewModel>();

        builder.Services.AddTransient<AddEditPropertyPage>();
        builder.Services.AddTransient<AddEditPropertyPageViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
