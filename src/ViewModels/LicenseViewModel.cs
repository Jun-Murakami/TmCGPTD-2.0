using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using TmCGPTD.Views;
using TmCGPTD.Models;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Markup.Xaml;
using System.Linq;
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
            using var streamReader = new StreamReader(AssetLoader.Open(new Uri("avares://TmCGPTD/Assets/License.txt")));
            LicenseText = await streamReader.ReadToEndAsync();
        }
    }
}