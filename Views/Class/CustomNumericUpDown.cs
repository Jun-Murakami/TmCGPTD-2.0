using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;
using System;
using Avalonia.Controls.Primitives;
using Avalonia;

namespace TmCGPTD
{
    public class CustomNumericUpDown : NumericUpDown, IStyleable
    {
        private TextBox _textBox;

        Type IStyleable.StyleKey => typeof(NumericUpDown);

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _textBox = e.NameScope.Find<TextBox>("PART_TextBox");
            if (_textBox != null)
            {
                _textBox.KeyDown += TextBox_KeyDown;
            }

            this.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var key = e.Key;
            var isCtrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);

            // 数字キーや小数点、制御キー（矢印キーやバックスペースなど）を許可する
            if ((key >= Avalonia.Input.Key.D0 && key <= Avalonia.Input.Key.D9) || (key >= Avalonia.Input.Key.NumPad0 && key <= Avalonia.Input.Key.NumPad9) ||
                key == Avalonia.Input.Key.Decimal || key == Avalonia.Input.Key.OemPeriod || key == Avalonia.Input.Key.Left || key == Avalonia.Input.Key.Right || key == Avalonia.Input.Key.Back ||
                key == Avalonia.Input.Key.Delete || key == Avalonia.Input.Key.Tab || isCtrl)
            {
                // 入力を許可する
            }
            else
            {
                e.Handled = true;
            }
        }

        private void OnDetachedFromVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            if (_textBox != null)
            {
                _textBox.KeyDown -= TextBox_KeyDown;
            }
            this.DetachedFromVisualTree -= OnDetachedFromVisualTree;
        }
    }


}