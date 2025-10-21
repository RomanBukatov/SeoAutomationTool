using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SeoTool.Core.Abstractions;
using SeoTool.Domain.Entities;

namespace SeoTool.Infrastructure.Services
{
    public class FileCookieProvider : ICookieProvider
    {
        private readonly string _sourceDirectoryPath;
        private readonly string _usedDirectoryPath;

        public FileCookieProvider(string sourceDirectoryPath, string usedDirectoryPath)
        {
            _sourceDirectoryPath = sourceDirectoryPath ?? throw new ArgumentNullException(nameof(sourceDirectoryPath));
            _usedDirectoryPath = usedDirectoryPath ?? throw new ArgumentNullException(nameof(usedDirectoryPath));
        }

        public async Task<IEnumerable<CookieInfo>> GetNextCookiesAsync()
        {
            if (!Directory.Exists(_sourceDirectoryPath))
            {
                throw new DirectoryNotFoundException($"Source directory not found: {_sourceDirectoryPath}");
            }

            var files = Directory.GetFiles(_sourceDirectoryPath);

            if (files.Length == 0)
            {
                throw new InvalidOperationException($"Source directory is empty: {_sourceDirectoryPath}");
            }

            var random = new Random();
            var randomFile = files[random.Next(files.Length)];

            var jsonContent = await File.ReadAllTextAsync(randomFile);
            var cookies = JsonSerializer.Deserialize<List<CookieInfo>>(jsonContent);

            if (cookies == null)
            {
                throw new InvalidOperationException($"Failed to deserialize cookies from file: {randomFile}");
            }

            if (!Directory.Exists(_usedDirectoryPath))
            {
                Directory.CreateDirectory(_usedDirectoryPath);
            }

            var fileName = Path.GetFileName(randomFile);
            var destinationPath = Path.Combine(_usedDirectoryPath, fileName);

            File.Move(randomFile, destinationPath);

            return cookies;
        }
    }
}