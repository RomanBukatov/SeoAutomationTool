using System.Collections.Generic;
using System.Threading.Tasks;
using SeoTool.Domain.Entities;

namespace SeoTool.Core.Abstractions
{
    public interface ICookieProvider
    {
        Task<IEnumerable<CookieInfo>> GetNextCookiesAsync(string folderPath, string usedCookiesFolderPath);
    }
}