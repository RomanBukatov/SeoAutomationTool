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
        private readonly string _filePath;

        public FileProxyProvider(string filePath)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }

        public async Task<Proxy> GetNextProxyAsync()
        {
            if (!File.Exists(_filePath))
            {
                throw new FileNotFoundException($"Proxy file not found: {_filePath}");
            }

            var lines = await File.ReadAllLinesAsync(_filePath);

            if (lines.Length == 0)
            {
                throw new InvalidOperationException($"Proxy file is empty: {_filePath}");
            }

            var random = new Random();
            var randomLine = lines[random.Next(lines.Length)].Trim();

            if (string.IsNullOrWhiteSpace(randomLine))
            {
                throw new InvalidOperationException($"Selected proxy line is empty in file: {_filePath}");
            }

            return ParseProxy(randomLine);
        }

        private static Proxy ParseProxy(string proxyLine)
        {
            var parts = proxyLine.Split(':');

            if (parts.Length < 2)
            {
                throw new FormatException($"Invalid proxy format: {proxyLine}. Expected format: host:port[:user:pass]");
            }

            if (!int.TryParse(parts[1], out var port))
            {
                throw new FormatException($"Invalid port number: {parts[1]} in proxy: {proxyLine}");
            }

            var host = parts[0];
            string? username = null;
            string? password = null;

            if (parts.Length >= 3)
            {
                username = parts[2];
            }

            if (parts.Length >= 4)
            {
                password = parts[3];
            }

            return new Proxy(host, port, username, password);
        }
    }
}