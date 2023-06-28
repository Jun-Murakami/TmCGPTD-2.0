using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using System;
using TmCGPTD.ViewModels;

namespace TmCGPTD.ViewModels
{
    public class ProgressViewModel : ViewModelBase
    {
        private ContentDialog? dialog;
        public ProgressViewModel()
        {

        }

        public void SetDialog(ContentDialog cdialog)
        {
            dialog = cdialog;
        }

        private string? _progressText;
        public string? ProgressText
        {
            get => _progressText;
            set => SetProperty(ref _progressText, value);
        }

        private double? _progressValue;
        public double? ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        public void Hide()
        {
            dialog.Hide();
        }
    }
}