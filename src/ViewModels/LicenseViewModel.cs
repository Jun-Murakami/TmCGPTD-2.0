using Avalonia;
using System;
using System.IO;
using System.Diagnostics;
using Avalonia.Platform;

namespace TmCGPTD.ViewModels
{
    public class LicenseViewModel : ViewModelBase
    {
        public LicenseViewModel()
        {
            LicenseLoad();
        }

        private string? _licenseText;
        public string? LicenseText
        {
            get => _licenseText;
            set => SetProperty(ref _licenseText, value);
        }

        private async void LicenseLoad()
        {
#if WINDOWS
            using var streamReader = new StreamReader(AssetLoader.Open(new Uri("avares://TmCGPTD/Assets/License.txt")));
#else
            using var streamReader = new StreamReader(AvaloniaLocator.Current.GetService<IAssetLoader>()!.Open(new Uri("avares://TmCGPTD/Assets/License.txt")));
#endif
            LicenseText = await streamReader.ReadToEndAsync();
        }
    }
}