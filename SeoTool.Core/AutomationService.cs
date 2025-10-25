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

    public AutomationService(
        IProxyProvider proxyProvider,
        ICookieProvider cookieProvider,
        IBrowserWorker browserWorker)
    {
        _proxyProvider = proxyProvider;
        _cookieProvider = cookieProvider;
        _browserWorker = browserWorker;
    }

    public async Task StartAutomationAsync(
        SearchTask task,
        string proxyPath,
        string cookiesPath,
        string usedCookiesPath,
        CancellationToken token = default)
    {
        try
        {
            var proxy = await _proxyProvider.GetNextProxyAsync(proxyPath);
            var cookies = await _cookieProvider.GetNextCookiesAsync(cookiesPath, usedCookiesPath);

            await _browserWorker.PerformSearchTaskAsync(task, proxy, cookies, token);
        }
        catch (Exception ex)
        {
            // Здесь можно добавить логирование
            Console.WriteLine($"Ошибка в AutomationService: {ex.Message}");
            throw; // Перебрасываем исключение выше, чтобы ViewModel его поймал
        }
    }
}