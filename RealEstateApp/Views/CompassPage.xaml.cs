using RealEstateApp.ViewModels;

namespace RealEstateApp.Views;

public partial class CompassPage : ContentPage
{
    private CompassPageViewModel ViewModel => BindingContext as CompassPageViewModel;

    public CompassPage(CompassPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (ViewModel != null)
        {
            await ViewModel.OnAppearing();
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        if (ViewModel != null)
        {
            await ViewModel.StopCompass();
        }
    }
}