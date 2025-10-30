try
{
    Console.WriteLine("Playwright browser installation started. Please wait...");
    int exitCode = Microsoft.Playwright.Program.Main(new[] { "install" });
    if (exitCode != 0)
    {
        Console.WriteLine($"Error: Installation failed with exit code {exitCode}");
    }
    else
    {
        Console.WriteLine("Installation completed successfully!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
}
Console.WriteLine("You can close this window now.");
Console.ReadKey();
