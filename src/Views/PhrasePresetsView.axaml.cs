using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;
using TmCGPTD.ViewModels;

namespace TmCGPTD.Views
{
    public partial class PhrasePresetsView : UserControl
    {
        public PhrasePresetsViewModel PhrasePresetsViewModel { get; } = new PhrasePresetsViewModel();
        public PhrasePresetsView()
        {
            InitializeComponent();
            DataContext = PhrasePresetsViewModel;
            VMLocator.PhrasePresetsViewModel = PhrasePresetsViewModel;

            PhrasePresetsViewModel.PropertyChanged += ViewModel_PropertyChanged!;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            int start, end;
            bool isKeyDown = false;
            bool isKeyCommand = false;

            switch (e.PropertyName)
            {
                case nameof(PhrasePresetsViewModel.CtrlKeyIsDown):
                    start = 1;
                    end = 10;
                    isKeyDown = PhrasePresetsViewModel.CtrlKeyIsDown;
                    break;
                case nameof(PhrasePresetsViewModel.AltKeyIsDown):
                    start = 11;
                    end = 20;
                    isKeyDown = PhrasePresetsViewModel.AltKeyIsDown;
                    break;
                case nameof(PhrasePresetsViewModel.KeyDownNum):
                    start = PhrasePresetsViewModel.KeyDownNum;
                    end = PhrasePresetsViewModel.KeyDownNum;
                    isKeyCommand = true;
                    break;
                default:
                    return;
            }

            for (int i = start; i <= end; i++)
            {
                TextBox? textBox = this.FindControl<TextBox>($"TextBox{i}");
                Button? button = this.FindControl<Button>($"Button{i}");

                if(isKeyCommand)
                {
                    OnButtonClick(button!,null!);
                    return;
                }

                if (isKeyDown)
                {
                    textBox!.Classes.Add("KeyDown");
                    button!.Classes.Add("KeyDown");
                }
                else
                {
                    textBox!.Classes.Remove("KeyDown");
                    button!.Classes.Remove("KeyDown");
                }
            }
        }


        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                int buttonNumber = int.Parse(button.Name!.Substring(6))!;
                TextBox textBox = this.FindControl<TextBox>($"TextBox{buttonNumber}")!;

                if (textBox.Text == null ) return;

                // Get the currently focused control
                var focusManager = TopLevel.GetTopLevel(this)!.FocusManager;
                var focusedControl = focusManager?.GetFocusedElement();
                if (focusedControl == null) return;

                if (focusedControl is TextBox focusedTextBox)
                {
                    int start = focusedTextBox.SelectionStart;
                    int length = focusedTextBox.SelectionEnd - focusedTextBox.SelectionStart;

                    // if text is null, set empty string
                    if (focusedTextBox.Text == null)
                    {
                        focusedTextBox.Text = string.Empty;
                    }

                    if (length != 0)
                    {
                        if (length < 0)
                        {
                            length = Math.Abs(length);
                            start = start - length;
                        }
                        // if selected text is not empty, remove selected text
                        focusedTextBox.Text = focusedTextBox.Text.Remove(start, length);
                    }

                    // focus on the end of inserted text
                    focusedTextBox.Text = focusedTextBox.Text.Insert(start, textBox.Text);
                    focusedTextBox.CaretIndex = start + textBox.Text.Length;
                }
                else if (focusedControl is AvaloniaEdit.Editing.TextArea focusedTextArea)
                {
                        focusedTextArea.Selection.ReplaceSelectionWithText(textBox.Text);
                }
            }
        }
    }
}
