namespace SeoTool.Domain.Entities;

public record Proxy(string Host, int Port, string? Username, string? Password);

public record CookieInfo(string Name, string Value, string Domain, string Path);

public record SearchTask(string Domain, string Keyword);

public record Fingerprint(string Value);