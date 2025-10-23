using System.Configuration;
using System.Data;
using System.Windows;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using SeoTool.Core;
using SeoTool.Core.Abstractions;
using SeoTool.Infrastructure.Services;
using SeoTool.Wpf.ViewModels;

namespace SeoTool.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var serviceCollection = new ServiceCollection();

            // Регистрируем сервисы приложения
            serviceCollection.AddTransient<MainViewModel>();
            serviceCollection.AddTransient<MainWindow>();
            serviceCollection.AddTransient<AutomationService>();

            // Регистрируем провайдеры
            serviceCollection.AddTransient<IBrowserWorker, PlaywrightBrowserWorker>();
            serviceCollection.AddSingleton<IProxyProvider, FileProxyProvider>();
            serviceCollection.AddSingleton<ICookieProvider, FileCookieProvider>();

            // Регистрируем HttpClient как singleton
            serviceCollection.AddSingleton<HttpClient>(new HttpClient());

            // Создаем провайдер сервисов
            ServiceProvider = serviceCollection.BuildServiceProvider();

            // Получаем и показываем главное окно
            try
            {
                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting application: {ex.Message}\nStackTrace: {ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }
    }

}
