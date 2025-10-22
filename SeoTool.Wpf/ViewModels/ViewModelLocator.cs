using System;
using Microsoft.Extensions.DependencyInjection;

namespace SeoTool.Wpf.ViewModels
{
    public class ViewModelLocator
    {
        public MainViewModel MainViewModel => App.ServiceProvider?.GetRequiredService<MainViewModel>();
    }
}