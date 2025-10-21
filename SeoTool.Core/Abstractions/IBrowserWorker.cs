using System.Collections.Generic;
using System.Threading.Tasks;
using SeoTool.Domain.Entities;

namespace SeoTool.Core.Abstractions
{
    public interface IBrowserWorker
    {
        Task PerformSearchTaskAsync(SearchTask task, Proxy proxy, IEnumerable<CookieInfo> cookies, Fingerprint fingerprint);
    }
}