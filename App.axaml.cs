using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using muzzstream.ViewModels;
using muzzstream.Views;

namespace muzzstream;

public partial class App : Application {
    public override void Initialize() {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = new MainWindow {
                DataContext = new MainViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
