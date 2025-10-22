using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeoTool.Core;
using SeoTool.Domain.Entities;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Win32;
using System.Windows;
using System.Runtime.InteropServices;

namespace SeoTool.Wpf.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly AutomationService _automationService;
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
        private string _configFilePath;

        [ObservableProperty]
        private string _logs;

        public MainViewModel(AutomationService automationService)
        {
            _automationService = automationService;
        }

        [RelayCommand(CanExecute = nameof(CanStartAutomation))]
        private async Task StartAutomation()
        {
            // Dispose of any existing cancellation token source
            _cts?.Dispose();

            try
            {
                // Create new cancellation token source
                _cts = new CancellationTokenSource();
                var task = new SearchTask("example.com", "keyword"); // Using placeholder data as specified
                await _automationService.StartAutomationAsync(task, ProxyFilePath, CookiesFolderPath, UsedCookiesFolderPath, _cts.Token);
            }
            catch (Exception ex)
            {
                var userFriendlyMessage = GetUserFriendlyErrorMessage(ex);
                Logs += $"Error: {userFriendlyMessage}\n";
                MessageBox.Show($"Произошла ошибка при выполнении автоматизации:\n\n{userFriendlyMessage}",
                    "Ошибка автоматизации", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Logs += "Work completed.\n";
            }
        }

        [RelayCommand]
        private void StopAutomationCommand()
        {
            _cts?.Cancel();
            Logs += "Automation stopped.\n";
        }

        private bool CanStartAutomation()
        {
            return !string.IsNullOrWhiteSpace(ProxyFilePath) &&
                   !string.IsNullOrWhiteSpace(CookiesFolderPath) &&
                   !string.IsNullOrWhiteSpace(UsedCookiesFolderPath) &&
                   !string.IsNullOrWhiteSpace(FingerprintApiKey) &&
                   !string.IsNullOrWhiteSpace(ConfigFilePath);
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

        partial void OnConfigFilePathChanged(string value)
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
        private void SelectConfigFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите файл конфигурации",
                Filter = "Файлы конфигурации (*.json;*.config;*.ini;*.txt)|*.json;*.config;*.ini;*.txt|Все файлы (*.*)|*.*",
                CheckFileExists = false,
                CheckPathExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ConfigFilePath = openFileDialog.FileName;
            }
        }

        [RelayCommand]
        private void SelectFingerprintApiKey()
        {
            // Для API ключа проще позволить пользователю ввести его вручную
            // Можно показать диалоговое окно с подсказкой или просто установить фокус на поле
            MessageBox.Show(
                "Пожалуйста, введите ваш API-ключ от FingerprintSwitcher в поле выше.\n\n" +
                "Вы можете получить ключ в личном кабинете сервиса FingerprintSwitcher.",
                "API-ключ Fingerprint",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
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