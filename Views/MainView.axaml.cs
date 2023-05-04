using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using TmCGPTD.ViewModels;

namespace TmCGPTD.Views
{
    public partial class MainView : UserControl
    {
        public MainViewModel MainViewModel { get; } = new MainViewModel();
        private StackPanel _stackPanel;
        public MainView()
        {
            InitializeComponent();

            DataContext = MainViewModel;
            VMLocator.MainViewModel = MainViewModel;

            _stackPanel = this.FindControl<StackPanel>("ProgramTitleBar");

            if (OperatingSystem.IsMacOS())
            {
                _stackPanel.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
