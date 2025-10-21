using System.Threading.Tasks;
using SeoTool.Domain.Entities;

namespace SeoTool.Core.Abstractions
{
    public interface IProxyProvider
    {
        Task<Proxy> GetNextProxyAsync();
    }
}