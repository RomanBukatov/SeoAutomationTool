using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SeoTool.Core.Abstractions;
using SeoTool.Domain.Entities;

namespace SeoTool.Infrastructure.Services
{
    public class FileProxyProvider : IProxyProvider
    {
        public FileProxyProvider()
        {
        }

        public async Task<Proxy> GetNextProxyAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Proxy file not found: {filePath}");
            }

            var lines = (await File.ReadAllLinesAsync(filePath)).ToList();

            if (lines.Count == 0)
            {
                throw new InvalidOperationException($"Proxy file is empty: {filePath}");
            }

            var proxyLine = lines[0].Trim();

            if (string.IsNullOrWhiteSpace(proxyLine))
            {
                throw new InvalidOperationException($"Selected proxy line is empty in file: {filePath}");
            }

            var remainingLines = lines.Skip(1).ToList();
            await File.WriteAllLinesAsync(filePath, remainingLines);

            return ParseProxy(proxyLine);
        }

        private static Proxy ParseProxy(string proxyLine)
        {
            var parts = proxyLine.Split(':');

            if (parts.Length < 2)
            {
                throw new FormatException($"Invalid proxy format: {proxyLine}. Expected format: host:port[:user:pass]");
            }

            // Берем только первые 4 части, остальное игнорируем
            var limitedParts = parts.Take(4).ToArray();

            if (!int.TryParse(limitedParts[1], out var port))
            {
                throw new FormatException($"Invalid port number: {limitedParts[1]} in proxy: {proxyLine}");
            }

            var host = limitedParts[0];
            string? username = null;
            string? password = null;

            if (limitedParts.Length >= 3)
            {
                username = limitedParts[2];
            }

            if (limitedParts.Length >= 4)
            {
                password = limitedParts[3];
            }

            return new Proxy(host, port, username, password);
        }
    }
}