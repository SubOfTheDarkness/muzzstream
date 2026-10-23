using Avalonia.Controls;
using muzzstream.ViewModels;

namespace muzzstream.Views;

public partial class ProfileView : UserControl
{
    public ProfileView()
    {
        InitializeComponent();

        this.AttachedToVisualTree += (s, e) =>
        {
            if (DataContext is MainViewModel vm)
            {
                RoleBadge.IsVisible = vm.CurrentUser?.role_id > 0;
            }
        };
    }
}
