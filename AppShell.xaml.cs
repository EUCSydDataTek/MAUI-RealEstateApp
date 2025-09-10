using RealEstateApp.Views;

namespace RealEstateApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(PropertyDetailPage), typeof(PropertyDetailPage));
        Routing.RegisterRoute(nameof(AddEditPropertyPage), typeof(AddEditPropertyPage));
        Routing.RegisterRoute(nameof(CompassPage), typeof(CompassPage));
        Routing.RegisterRoute(nameof(ImageListPage), typeof(ImageListPage));
    }
}