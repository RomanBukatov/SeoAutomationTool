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
            Console.WriteLine(">>> AutomationService.StartAutomationAsync START");
            var fingerprint = await _fingerprintProvider.GetFingerprintAsync(fingerprintApiKey);
            // TODO: Pass fingerprint to browserWorker
        }
        catch (Exception ex)
        {
            // Здесь можно добавить логирование
            Console.WriteLine($"Ошибка в AutomationService: {ex.Message}");
            throw; // Перебрасываем исключение выше, чтобы ViewModel его поймал
        }
    }
}