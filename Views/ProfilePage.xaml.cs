using GamerLinkApp.ViewModels;

namespace GamerLinkApp.Views;

public partial class ProfilePage : ContentPage
{
    public ProfilePage(ProfileViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
    public ProfilePage()
    {
        InitializeComponent();
    }
}