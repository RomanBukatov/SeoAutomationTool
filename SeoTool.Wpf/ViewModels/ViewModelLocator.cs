using Microsoft.Extensions.DependencyInjection;
using SeoTool.Wpf.ViewModels;

namespace SeoTool.Wpf.ViewModels
{
    public class ViewModelLocator
    {
        public MainViewModel MainViewModel => SeoTool.Wpf.App.ServiceProvider?.GetRequiredService<MainViewModel>() ?? throw new InvalidOperationException("ServiceProvider is not initialized");
    }
}