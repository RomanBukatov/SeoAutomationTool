using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using SeoTool.Domain.Entities;

namespace SeoTool.Core.Abstractions
{
    public interface IBrowserWorker
    {
        Task PerformSearchTaskAsync(SearchTask task, Proxy proxy, IEnumerable<CookieInfo> cookies, CancellationToken cancellationToken = default);
    }
}