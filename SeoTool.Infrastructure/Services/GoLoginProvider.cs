using SeoTool.Core.Abstractions;
using SeoTool.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SeoTool.Infrastructure.Services;

public class GoLoginProvider : IFingerprintProvider
{
    public async Task<Fingerprint> GetFingerprintAsync(string apiKey)
    {
        Console.WriteLine(">>> GoLoginProvider.GetFingerprintAsync START");
        string tempJsonPath = Path.GetTempFileName();
        try
        {
            Console.WriteLine(">>> GoLoginProvider: Создание временного JSON файла");
            // --- ЭТАП 1: Создание профиля ---
            var requestBody = new
            {
                name = $"SeoToolProfile_{Random.Shared.Next(1000, 9999)}",
                os = "win",
                browserType = "chrome",
                navigator = new {
                    resolution = "1920x1080",
                    language = "en-US,en;q=0.9",
                    userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Safari/537.36",
                    platform = "Win32"
                },
                proxy = new {
                    mode = "none"
                }
            };
            await File.WriteAllTextAsync(tempJsonPath, JsonSerializer.Serialize(requestBody));
            Console.WriteLine(">>> GoLoginProvider: JSON файл создан, путь: " + tempJsonPath);

            Console.WriteLine(">>> GoLoginProvider: Выполнение POST запроса для создания профиля");
            var postIdResult = await RunCurlCommand($"-X POST \"https://api.gologin.com/browser\" -H \"Authorization: Bearer {apiKey}\" -H \"Content-Type: application/json\" -d @\"{tempJsonPath}\"");
            Console.WriteLine($"GoLogin POST response: {postIdResult}");

            Console.WriteLine(">>> GoLoginProvider: Парсинг ответа POST запроса");
            var profileId = JsonDocument.Parse(postIdResult).RootElement.GetProperty("id").GetString();
            Console.WriteLine(">>> GoLoginProvider: Получен profileId: " + profileId);

            if(string.IsNullOrEmpty(profileId))
                throw new InvalidOperationException("Не удалось получить ID профиля от GoLogin.");

            // --- ЭТАП 2: Получение отпечатка ---
            Console.WriteLine(">>> GoLoginProvider: Выполнение GET запроса для получения отпечатка");
            var getFingerprintResult = await RunCurlCommand($"-X GET \"https://api.gologin.com/browser/{profileId}\" -H \"Authorization: Bearer {apiKey}\"");
            Console.WriteLine($"GoLogin GET response: {getFingerprintResult}");
            Console.WriteLine(">>> GoLoginProvider: Парсинг ответа GET запроса");

            // --- Парсинг и формирование JS ---
            using var jsonDoc = JsonDocument.Parse(getFingerprintResult);
            var rootData = jsonDoc.RootElement;

            // Извлекаем данные из navigator
            var navigatorData = rootData.GetProperty("navigator");
            var userAgent = navigatorData.GetProperty("userAgent").GetString();
            var platform = navigatorData.GetProperty("platform").GetString();
            var language = navigatorData.GetProperty("language").GetString();

            // Извлекаем разрешение экрана из navigator.resolution
            var resolutionStr = navigatorData.GetProperty("resolution").GetString();
            var resolutionParts = resolutionStr.Split('x');
            var screenWidth = int.Parse(resolutionParts[0]);
            var screenHeight = int.Parse(resolutionParts[1]);

            // Извлекаем данные из webGLMetadata
            var webglData = rootData.GetProperty("webGLMetadata");
            var webglVendor = webglData.GetProperty("vendor").GetString();
            var webglRenderer = webglData.GetProperty("renderer").GetString();

            // Извлекаем hardwareConcurrency и deviceMemory
            var hardwareConcurrency = navigatorData.GetProperty("hardwareConcurrency").GetInt32();
            var deviceMemory = navigatorData.GetProperty("deviceMemory").GetInt32();

            // Формируем JavaScript код для Playwright
            var jsCode = $@"
// Override navigator properties
Object.defineProperty(navigator, 'userAgent', {{
    get: () => '{userAgent}'
}});

Object.defineProperty(navigator, 'platform', {{
    get: () => '{platform}'
}});

Object.defineProperty(navigator, 'language', {{
    get: () => '{language}'
}});

Object.defineProperty(navigator, 'deviceMemory', {{
    get: () => {deviceMemory}
}});

Object.defineProperty(navigator, 'hardwareConcurrency', {{
    get: () => {hardwareConcurrency}
}});

// Override screen properties
Object.defineProperty(screen, 'width', {{
    get: () => {screenWidth}
}});

Object.defineProperty(screen, 'height', {{
    get: () => {screenHeight}
}});

// Override WebGL
const getParameter = WebGLRenderingContext.prototype.getParameter;
WebGLRenderingContext.prototype.getParameter = function(parameter) {{
    if (parameter === 37445) return '{webglVendor}'; // VENDOR
    if (parameter === 37446) return '{webglRenderer}'; // RENDERER
    return getParameter.call(this, parameter);
}};
";

            return new Fingerprint(jsCode);
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> GoLoginProvider: Исключение в GetFingerprintAsync: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            throw new InvalidOperationException($"Критическая ошибка при работе с GoLogin через cURL: {ex.Message}", ex);
        }
        finally
        {
            if (File.Exists(tempJsonPath))
                File.Delete(tempJsonPath);
        }
    }

    private async Task<string> RunCurlCommand(string arguments)
    {
        Console.WriteLine(">>> GoLoginProvider.RunCurlCommand: Запуск curl с аргументами: " + arguments);
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "curl.exe",
                    Arguments = arguments + " -v", // Добавляем verbose
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                }
            };

            Console.WriteLine(">>> GoLoginProvider.RunCurlCommand: Запуск процесса");
            process.Start();
            Console.WriteLine(">>> GoLoginProvider.RunCurlCommand: Чтение вывода");
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            Console.WriteLine(">>> GoLoginProvider.RunCurlCommand: Ожидание завершения");
            await process.WaitForExitAsync();

            Console.WriteLine($">>> GoLoginProvider.RunCurlCommand: Exit code: {process.ExitCode}");
            Console.WriteLine($">>> GoLoginProvider.RunCurlCommand: Error output: {error}");

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Ошибка выполнения cURL. Exit code: {process.ExitCode}. Ошибка: {error}");
            }

            // Очищаем ответ от мусора перед JSON скобкой
            Console.WriteLine("--- RAW CURL OUTPUT ---");
            Console.WriteLine(output);
            Console.WriteLine("--- END RAW CURL OUTPUT ---");

            int jsonStartIndex = output.IndexOf('{');
            if (jsonStartIndex >= 0)
            {
                output = output.Substring(jsonStartIndex);
                Console.WriteLine(">>> GoLoginProvider.RunCurlCommand: Очищенный output: " + output);
            }
            else
            {
                Console.WriteLine(">>> GoLoginProvider.RunCurlCommand: JSON скобка не найдена!");
            }

            return output;
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> GoLoginProvider.RunCurlCommand: Исключение: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }
}