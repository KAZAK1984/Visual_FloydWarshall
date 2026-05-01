using System.IO;
using System.Windows;
using Visual_FloydWarshall.Algorithm;
using Visual_FloydWarshall.Logging;

namespace Visual_FloydWarshall
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ILogger _logger = new FileLogger(Path.Combine(AppContext.BaseDirectory, "visual_floyd.log"));

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _logger.Info("Приложение запущено.");

            new MainWindow(new FloydAlgorithmRunner(_logger), _logger).Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _logger.Info("Приложение закрыто.");
            base.OnExit(e);
        }
    }

}
