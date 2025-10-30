using SeoTool.Domain.Entities;
using System.Threading;

namespace SeoTool.Core.Abstractions;

public interface IAutomationService
{
    Task StartAutomationAsync(
        SearchTask task,
        string proxyPath,
        string cookiesPath,
        string usedCookiesPath,
        string fingerprintApiKey,
        Action<string> logAction,
        CancellationToken token = default);
}