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
        Action<string> logAction,
        CancellationToken token = default)
    {
        try
        {
            var proxy = await _proxyProvider.GetNextProxyAsync(proxyPath);
            logAction("Прокси получен.");
            token.ThrowIfCancellationRequested();

            var cookies = await _cookieProvider.GetNextCookiesAsync(cookiesPath, usedCookiesPath);
            logAction("Куки получены и перемещены.");
            token.ThrowIfCancellationRequested();

            Fingerprint fingerprint;
            try
            {
                fingerprint = await _fingerprintProvider.GetFingerprintAsync(fingerprintApiKey);
                logAction("Отпечаток получен.");
                token.ThrowIfCancellationRequested();
            }
            catch (Exception ex)
            {
                throw;
            }

            await _browserWorker.PerformSearchTaskAsync(task, proxy, cookies, fingerprint, logAction, token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка в AutomationService: {ex.Message}");
            throw;
        }
    }
}