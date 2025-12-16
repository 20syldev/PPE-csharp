using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace PPE.Controller
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new Login();
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}
