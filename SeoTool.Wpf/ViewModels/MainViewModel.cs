using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeoTool.Core;
using SeoTool.Core.Abstractions;
using SeoTool.Domain.Entities;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Windows;
using System.Runtime.InteropServices;
using System.IO;

namespace SeoTool.Wpf.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IAutomationService _automationService;
        private readonly ILogger<MainViewModel> _logger;
        private CancellationTokenSource _cts;

        [ObservableProperty]
        private string _proxyFilePath;

        [ObservableProperty]
        private string _cookiesFolderPath;

        [ObservableProperty]
        private string _usedCookiesFolderPath;


        [ObservableProperty]
        private string _fingerprintApiKey;

        [ObservableProperty]
        private string _targetDomain;

        [ObservableProperty]
        private string _keyword;

        [ObservableProperty]
        private string _logs;

        public MainViewModel(IAutomationService automationService, ILogger<MainViewModel> logger)
        {
            _automationService = automationService;
            _logger = logger;
        }

        public void AddLog(string message)
        {
            Application.Current.Dispatcher.Invoke(() => Logs += $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        [RelayCommand]
        private async Task StartAutomation()
        {
            // Проверяем обязательные поля
            var missingFields = new List<string>();
            if (string.IsNullOrWhiteSpace(ProxyFilePath)) missingFields.Add("Путь к файлу прокси");
            if (string.IsNullOrWhiteSpace(CookiesFolderPath)) missingFields.Add("Папка с куки");
            if (string.IsNullOrWhiteSpace(UsedCookiesFolderPath)) missingFields.Add("Папка для использованных куки");
            if (string.IsNullOrWhiteSpace(TargetDomain)) missingFields.Add("Целевой домен");
            if (string.IsNullOrWhiteSpace(Keyword)) missingFields.Add("Ключевое слово");
            if (string.IsNullOrWhiteSpace(FingerprintApiKey)) missingFields.Add("API-ключ GoLogin");

            if (missingFields.Any())
            {
                MessageBox.Show($"Необходимо заполнить следующие поля:\n\n{string.Join("\n", missingFields)}",
                    "Недостаточно данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Dispose of any existing cancellation token source
            _cts?.Dispose();

            try
            {
                Logs += $"[{DateTime.Now:HH:mm:ss}] Запуск автоматизации для домена '{TargetDomain}' с ключевым словом '{Keyword}'...\n";

                // Create new cancellation token source
                _cts = new CancellationTokenSource();
                var task = new SearchTask(TargetDomain, Keyword);
                await _automationService.StartAutomationAsync(task, ProxyFilePath, CookiesFolderPath, UsedCookiesFolderPath, FingerprintApiKey, AddLog, _cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== STARTAUTOMATIONCOMMAND EXCEPTION ===");
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                Console.WriteLine($"=== END STARTAUTOMATIONCOMMAND EXCEPTION ===");

                var userFriendlyMessage = GetUserFriendlyErrorMessage(ex);
                Logs += $"[{DateTime.Now:HH:mm:ss}] Ошибка: {userFriendlyMessage}\n";
                _logger.LogError(ex, "Ошибка в StartAutomationCommand");
                MessageBox.Show($"Произошла ошибка при выполнении автоматизации:\n\n{userFriendlyMessage}",
                    "Ошибка автоматизации", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Logs += $"[{DateTime.Now:HH:mm:ss}] Автоматизация завершена.\n";
            }
        }

        [RelayCommand]
        private void StopAutomationCommand()
        {
            _cts?.Cancel();
            Logs += $"[{DateTime.Now:HH:mm:ss}] Автоматизация остановлена пользователем.\n";
        }


        partial void OnProxyFilePathChanged(string value)
        {
            StartAutomationCommand.NotifyCanExecuteChanged();
        }

        partial void OnCookiesFolderPathChanged(string value)
        {
            StartAutomationCommand.NotifyCanExecuteChanged();
        }

        partial void OnUsedCookiesFolderPathChanged(string value)
        {
            StartAutomationCommand.NotifyCanExecuteChanged();
        }



        partial void OnFingerprintApiKeyChanged(string value)
        {
            StartAutomationCommand.NotifyCanExecuteChanged();
        }

        partial void OnTargetDomainChanged(string value)
        {
            StartAutomationCommand.NotifyCanExecuteChanged();
        }

        partial void OnKeywordChanged(string value)
        {
            StartAutomationCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void SelectProxyFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите файл прокси",
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ProxyFilePath = openFileDialog.FileName;
            }
        }

        [RelayCommand]
        private void SelectCookiesFolder()
        {
            var folderPath = ShowFolderBrowserDialog("Выберите папку с куки");
            if (!string.IsNullOrEmpty(folderPath))
            {
                CookiesFolderPath = folderPath;
            }
        }

        [RelayCommand]
        private void SelectUsedCookiesFolder()
        {
            var folderPath = ShowFolderBrowserDialog("Выберите папку для использованных куки");
            if (!string.IsNullOrEmpty(folderPath))
            {
                UsedCookiesFolderPath = folderPath;
            }
        }

        private static string ShowFolderBrowserDialog(string description)
        {
            // Используем Windows API для создания диалога выбора папки
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = description,
                Filter = "Папки|*.none",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Выберите папку",
                ValidateNames = false
            };

            if (dialog.ShowDialog() == true)
            {
                // Извлекаем путь к папке из выбранного файла
                var folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                return folderPath;
            }

            return null;
        }


        [RelayCommand]
        private void ShowFingerprintHelp()
        {
            MessageBox.Show(
                "Для получения API ключа GoLogin:\n\n" +
                "1. Зарегистрируйтесь на сайте https://gologin.com\n" +
                "2. Перейдите в раздел API\n" +
                "3. Создайте новый API ключ\n" +
                "4. Скопируйте ключ и вставьте в поле выше\n\n" +
                "API ключ используется для получения уникальных отпечатков браузера.",
                "Справка по GoLogin API",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        [RelayCommand]
        private void ShowTargetDomainHelp()
        {
            MessageBox.Show(
                "Целевой домен:\n\n" +
                "Укажите домен сайта, который нужно найти в результатах поиска Bing.\n\n" +
                "Примеры:\n" +
                "• example.com\n" +
                "• shop.example.ru\n" +
                "• subdomain.site.org\n\n" +
                "Бот будет искать ссылки, содержащие этот домен, в результатах поиска.",
                "Подсказка по целевому домену",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        [RelayCommand]
        private void ShowKeywordHelp()
        {
            MessageBox.Show(
                "Ключевое слово:\n\n" +
                "Укажите фразу для поиска в Bing.\n\n" +
                "Примеры:\n" +
                "• купить смартфон\n" +
                "• лучшие рестораны Москвы\n" +
                "• как приготовить борщ\n\n" +
                "Бот выполнит поиск по этому запросу и перейдет по первой найденной ссылке на указанный домен.",
                "Подсказка по ключевому слову",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        [RelayCommand]
        private void ClearLogs()
        {
            Logs = string.Empty;
        }


        private static string GetUserFriendlyErrorMessage(Exception ex)
        {
            return ex.Message switch
            {
                _ when ex.Message.Contains("No .txt files found") => "В папке с куки не найдены .txt файлы. Убедитесь, что в папке есть файлы куки в формате Netscape.",
                _ when ex.Message.Contains("Source directory not found") => "Папка с куки не найдена. Проверьте правильность пути к папке.",
                _ when ex.Message.Contains("Proxy file not found") => "Файл прокси не найден. Проверьте правильность пути к файлу.",
                _ when ex.Message.Contains("Proxy file is empty") => "Файл прокси пуст. Добавьте прокси в файл.",
                _ when ex.Message.Contains("Invalid proxy format") => "Неверный формат прокси в файле. Используйте формат: host:port или host:port:username:password",
                _ when ex.Message.Contains("Invalid port number") => "Неверный номер порта в прокси. Убедитесь, что порт указан числом.",
                _ => $"Произошла ошибка: {ex.Message}"
            };
        }
    }
}