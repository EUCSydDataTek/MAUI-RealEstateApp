using RealEstateApp.ViewModels;

namespace RealEstateApp.Views;

public partial class HeightCalculatorPage : ContentPage
{
    private HeightCalculatorPageViewModel ViewModel => BindingContext as HeightCalculatorPageViewModel;

    public HeightCalculatorPage(HeightCalculatorPageViewModel vm)
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
            await ViewModel.StopBarometer();
        }
    }
}