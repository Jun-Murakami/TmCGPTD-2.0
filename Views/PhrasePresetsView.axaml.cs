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
                int buttonNumber = int.Parse(button.Name.Substring(6)); // "Button"�̌�̔ԍ����擾
                TextBox textBox = this.FindControl<TextBox>($"TextBox{buttonNumber}");

                if (textBox.Text == null )
                {
                    return;
                }

                // �t�H�[�J�X������R���g���[�����擾
                var focusedControl = FocusManager.Instance.Current;

                if (focusedControl is TextBox focusedTextBox)
                {
                    int start = focusedTextBox.SelectionStart;
                    int length = focusedTextBox.SelectionEnd - focusedTextBox.SelectionStart;
                    //Debug.WriteLine("start:" + start + " length:" + length);
                    // �e�L�X�g��null�̏ꍇ�A�󕶎���ɐݒ�
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
                        // �e�L�X�g�I��͈͂�����ꍇ�A�㏑��
                        focusedTextBox.Text = focusedTextBox.Text.Remove(start, length);
                    }

                    // �t�H�[�J�X������TextBox�Ƀe�L�X�g��}��
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
