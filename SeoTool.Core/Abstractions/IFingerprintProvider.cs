using System.Threading.Tasks;
using SeoTool.Domain.Entities;

namespace SeoTool.Core.Abstractions
{
    public interface IFingerprintProvider
    {
        Task<Fingerprint> GetFingerprintAsync();
    }
}