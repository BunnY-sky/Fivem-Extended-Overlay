using System.Windows;

namespace FiveMOverlay;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Login is required on every launch. The main UI is not created until OAuth succeeds.
        var login = new VioVWindow(requireLogin: true);
        bool? authenticated = login.ShowDialog();
        if (authenticated != true || string.IsNullOrWhiteSpace(login.AccessToken))
        {
            Shutdown();
            return;
        }

        var main = new MainWindow(login.AccessToken);
        MainWindow = main;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        main.Show();
    }
}
