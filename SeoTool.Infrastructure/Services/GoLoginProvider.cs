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
        string tempJsonPath = Path.GetTempFileName();
        try
        {
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
            var postIdResult = await RunCurlCommand($"-X POST \"https://api.gologin.com/browser\" -H \"Authorization: Bearer {apiKey}\" -H \"Content-Type: application/json\" -d @\"{tempJsonPath}\"");

            var profileId = JsonDocument.Parse(postIdResult).RootElement.GetProperty("id").GetString();

            if(string.IsNullOrEmpty(profileId))
                throw new InvalidOperationException("Не удалось получить ID профиля от GoLogin.");

            // --- ЭТАП 2: Получение отпечатка ---
            var getFingerprintResult = await RunCurlCommand($"-X GET \"https://api.gologin.com/browser/{profileId}\" -H \"Authorization: Bearer {apiKey}\"");

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
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "curl.exe",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            }
        };

        process.Start();
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Ошибка выполнения cURL. Exit code: {process.ExitCode}. Ошибка: {error}");
        }

        int jsonStartIndex = output.IndexOf('{');
        if (jsonStartIndex >= 0)
        {
            output = output.Substring(jsonStartIndex);
        }
        else
        {
            // Если JSON не найден, значит, сервер вернул ошибку в виде текста.
            // Мы должны упасть с понятным сообщением.
            throw new InvalidOperationException($"Ответ от cURL не содержит JSON. Вероятно, это ошибка API. Ответ: {output}");
        }

        return output;
    }
}