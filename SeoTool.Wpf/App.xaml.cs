using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            // Глобальный обработчик неперехваченных исключений
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var exception = args.ExceptionObject as Exception;
                Console.WriteLine($"=== UNHANDLED EXCEPTION ===");
                Console.WriteLine($"Exception: {exception?.Message}");
                Console.WriteLine($"StackTrace: {exception?.StackTrace}");
                Console.WriteLine($"IsTerminating: {args.IsTerminating}");
                Console.WriteLine($"=== END UNHANDLED EXCEPTION ===");
            };

            base.OnStartup(e);

            var serviceCollection = new ServiceCollection();

            // Добавляем логирование
            serviceCollection.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.SetMinimumLevel(LogLevel.Debug);
            });

            // Регистрируем сервисы приложения
            serviceCollection.AddTransient<MainViewModel>();
            serviceCollection.AddTransient<MainWindow>();
            serviceCollection.AddTransient<IAutomationService, AutomationService>();

            // Регистрируем провайдеры
            serviceCollection.AddTransient<IBrowserWorker, PlaywrightBrowserWorker>();
            serviceCollection.AddSingleton<IProxyProvider, FileProxyProvider>();
            serviceCollection.AddSingleton<ICookieProvider, FileCookieProvider>();
            serviceCollection.AddSingleton<IFingerprintProvider, GoLoginProvider>();


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
