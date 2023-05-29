using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
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
