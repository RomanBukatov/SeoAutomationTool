using SeoTool.Domain.Entities;
using System.Threading.Tasks;

namespace SeoTool.Core.Abstractions;

public interface IFingerprintProvider
{
    Task<Fingerprint> GetFingerprintAsync(string apiKey);
}