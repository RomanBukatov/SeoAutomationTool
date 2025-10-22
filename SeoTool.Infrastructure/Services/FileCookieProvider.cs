using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SeoTool.Core.Abstractions;
using SeoTool.Domain.Entities;

namespace SeoTool.Infrastructure.Services
{
    public class FileCookieProvider : ICookieProvider
    {
        public FileCookieProvider()
        {
        }

        public async Task<IEnumerable<CookieInfo>> GetNextCookiesAsync(string folderPath, string usedCookiesFolderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                throw new ArgumentNullException(nameof(folderPath));
            }

            if (string.IsNullOrWhiteSpace(usedCookiesFolderPath))
            {
                throw new ArgumentNullException(nameof(usedCookiesFolderPath));
            }

            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"Source directory not found: {folderPath}");
            }

            // Получаем только .txt файлы
            var files = Directory.GetFiles(folderPath, "*.txt");

            if (files.Length == 0)
            {
                throw new InvalidOperationException($"No .txt files found in directory: {folderPath}");
            }

            var random = new Random();
            var randomFile = files[random.Next(files.Length)];

            var cookies = await ParseNetscapeCookiesFileAsync(randomFile);

            if (cookies.Any())
            {
                // После успешного чтения перемещаем файл в папку использованных куки
                try
                {
                    if (!Directory.Exists(usedCookiesFolderPath))
                    {
                        Directory.CreateDirectory(usedCookiesFolderPath);
                    }

                    var fileName = Path.GetFileName(randomFile);
                    var destinationPath = Path.Combine(usedCookiesFolderPath, fileName);
                    File.Move(randomFile, destinationPath);
                }
                catch (Exception ex)
                {
                    // Логируем ошибку перемещения, но не прерываем выполнение
                    Console.WriteLine($"Warning: Failed to move cookies file to used folder: {ex.Message}");
                }
            }

            return cookies;
        }

        private async Task<IEnumerable<CookieInfo>> ParseNetscapeCookiesFileAsync(string filePath)
        {
            var cookies = new List<CookieInfo>();

            try
            {
                var lines = await File.ReadAllLinesAsync(filePath);

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    // Пропускаем пустые строки и комментарии (начинающиеся с #)
                    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                    {
                        continue;
                    }

                    // Разделяем по табуляции
                    var parts = trimmedLine.Split('\t');

                    // Проверяем, что у нас минимум 7 колонок (формат Netscape)
                    if (parts.Length >= 7)
                    {
                        var domain = parts[0];
                        var path = parts[2];
                        var name = parts[5];
                        var value = parts[6];

                        // Создаем объект CookieInfo только если есть имя куки
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            cookies.Add(new CookieInfo(name, value, domain, path));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error reading cookies file {filePath}: {ex.Message}", ex);
            }

            return cookies;
        }
    }
}