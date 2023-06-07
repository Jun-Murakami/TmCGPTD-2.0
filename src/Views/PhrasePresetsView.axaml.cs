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

            PhrasePresetsViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PhrasePresetsViewModel.CtrlKeyIsDown))
            {
                if (PhrasePresetsViewModel.CtrlKeyIsDown)
                {
                    //1から10まで繰り返してTextBoxのインスタンスを取得
                    for (int i = 1; i <= 10; i++)
                    {
                        TextBox textBox = this.FindControl<TextBox>($"TextBox{i}");
                        textBox.Classes.Add("KeyDown");
                        Button　button = this.FindControl<Button>($"Button{i}");
                        button.Classes.Add("KeyDown");

                    }
                }
                else
                {
                    for (int i = 1; i <= 10; i++)
                    {
                        TextBox textBox = this.FindControl<TextBox>($"TextBox{i}");
                        textBox.Classes.Remove("KeyDown");
                        Button button = this.FindControl<Button>($"Button{i}");
                        button.Classes.Remove("KeyDown");
                    }
                }
            }
            else if (e.PropertyName == nameof(PhrasePresetsViewModel.AltKeyIsDown))
            {
                if (PhrasePresetsViewModel.AltKeyIsDown)
                {
                    //11から20まで繰り返してTextBoxのインスタンスを取得
                    for (int i = 11; i <= 20; i++)
                    {
                        TextBox textBox = this.FindControl<TextBox>($"TextBox{i}");
                        textBox.Classes.Add("KeyDown");
                        Button button = this.FindControl<Button>($"Button{i}");
                        button.Classes.Add("KeyDown");
                    }
                }
                else
                {
                    for (int i = 11; i <= 20; i++)
                    {
                        TextBox textBox = this.FindControl<TextBox>($"TextBox{i}");
                        textBox.Classes.Remove("KeyDown");
                        Button button = this.FindControl<Button>($"Button{i}");
                        button.Classes.Remove("KeyDown");
                    }
                }
            }
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                int buttonNumber = int.Parse(button.Name.Substring(6)); // "Button"の後の番号を取得
                TextBox textBox = this.FindControl<TextBox>($"TextBox{buttonNumber}");

                if (textBox.Text == null )
                {
                    return;
                }

                // フォーカスがあるコントロールを取得
                var focusedControl = FocusManager.Instance.Current;

                if (focusedControl is TextBox focusedTextBox)
                {
                    int start = focusedTextBox.SelectionStart;
                    int length = focusedTextBox.SelectionEnd - focusedTextBox.SelectionStart;
                    //Debug.WriteLine("start:" + start + " length:" + length);
                    // テキストがnullの場合、空文字列に設定
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
                        // テキスト選択範囲がある場合、上書き
                        focusedTextBox.Text = focusedTextBox.Text.Remove(start, length);
                    }

                    // フォーカスがあるTextBoxにテキストを挿入
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
