using SeoTool.Core.Abstractions;
using SeoTool.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SeoTool.Core;

public class AutomationService : IAutomationService
{
    private readonly IProxyProvider _proxyProvider;
    private readonly ICookieProvider _cookieProvider;
    private readonly IBrowserWorker _browserWorker;
    private readonly IFingerprintProvider _fingerprintProvider;

    public AutomationService(
        IProxyProvider proxyProvider,
        ICookieProvider cookieProvider,
        IBrowserWorker browserWorker,
        IFingerprintProvider fingerprintProvider)
    {
        _proxyProvider = proxyProvider;
        _cookieProvider = cookieProvider;
        _browserWorker = browserWorker;
        _fingerprintProvider = fingerprintProvider;
    }

    public async Task StartAutomationAsync(
        SearchTask task,
        string proxyPath,
        string cookiesPath,
        string usedCookiesPath,
        string fingerprintApiKey,
        CancellationToken token = default)
    {
        try
        {
            Console.WriteLine(">>> AutomationService: Получаю прокси...");
            var proxy = await _proxyProvider.GetNextProxyAsync(proxyPath);
            token.ThrowIfCancellationRequested();
            Console.WriteLine(">>> AutomationService: Прокси получен успешно");

            Console.WriteLine(">>> AutomationService: Получаю куки...");
            var cookies = await _cookieProvider.GetNextCookiesAsync(cookiesPath, usedCookiesPath);
            token.ThrowIfCancellationRequested();
            Console.WriteLine(">>> AutomationService: Куки получены успешно");

            Console.WriteLine(">>> AutomationService: Получаю отпечаток...");
            Fingerprint fingerprint;
            try
            {
                fingerprint = await _fingerprintProvider.GetFingerprintAsync(fingerprintApiKey);
                token.ThrowIfCancellationRequested();
                Console.WriteLine(">>> AutomationService: Отпечаток получен успешно");
            }
            catch (Exception ex)
            {
                Console.WriteLine($">>> AutomationService: Ошибка при получении отпечатка: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                throw;
            }

            Console.WriteLine(">>> AutomationService: Запускаю воркер...");
            await _browserWorker.PerformSearchTaskAsync(task, proxy, cookies, fingerprint, token);
            Console.WriteLine(">>> AutomationService: Воркер завершил работу успешно");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(">>> AutomationService: Операция отменена пользователем.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка в AutomationService: {ex.Message}");
            throw;
        }
    }
}