using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace TmCGPTD.Views
{
    public partial class EditorThreeView : UserControl
    {
        public EditorThreeView()
        {
            InitializeComponent();

            this.AttachedToVisualTree += (sender, e) =>
            {
                var br = Environment.NewLine;
                if (VMLocator.EditorViewModel == null || VMLocator.EditorViewModel.GetRecentText() == null) return;
                VMLocator.EditorViewModel.Editor3Text = (VMLocator.EditorViewModel.Editor3Text + br + VMLocator.EditorViewModel.Editor4Text + br + VMLocator.EditorViewModel.Editor5Text).Trim();
                VMLocator.EditorViewModel.Editor4Text = string.Empty;
                VMLocator.EditorViewModel.Editor5Text = string.Empty;
            };
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
