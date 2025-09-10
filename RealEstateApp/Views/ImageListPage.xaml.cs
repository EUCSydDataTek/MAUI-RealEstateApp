using RealEstateApp.ViewModels;

namespace RealEstateApp.Views;

public partial class ImageListPage : ContentPage
{
    private ImageListPageViewModel ViewModel => BindingContext as ImageListPageViewModel;

    public ImageListPage(ImageListPageViewModel vm)
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
            await ViewModel.OnDisappearing();
        }
    }
}