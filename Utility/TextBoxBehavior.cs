using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Visual_FloydWarshall.Utility
{
	public static class TextBoxBehavior
	{
		public static readonly DependencyProperty NumericOnlyProperty =
			DependencyProperty.RegisterAttached(
				"NumericOnly",
				typeof(bool),
				typeof(TextBoxBehavior),
				new PropertyMetadata(false, OnDependencyPropertyChanged));

		public static readonly DependencyProperty MinValueProperty =
			DependencyProperty.RegisterAttached(
				"MinValue",
				typeof(int?),
				typeof(TextBoxBehavior),
				new PropertyMetadata(OnDependencyPropertyChanged));

		public static readonly DependencyProperty MaxValueProperty =
			DependencyProperty.RegisterAttached(
				"MaxValue",
				typeof(int?),
				typeof(TextBoxBehavior),
				new PropertyMetadata(OnDependencyPropertyChanged));

		public static bool GetNumericOnly(DependencyObject obj) =>
			(bool)obj.GetValue(NumericOnlyProperty);

		public static void SetNumericOnly(DependencyObject obj, bool value) =>
			obj.SetValue(NumericOnlyProperty, value);

		public static int? GetMaxValue(DependencyObject obj) =>
			(int?)obj.GetValue(MaxValueProperty);

		public static void SetMaxValue(DependencyObject obj, int? value) =>
			obj.SetValue(MaxValueProperty, value);

		public static int? GetMinValue(DependencyObject obj) =>
			(int?)obj.GetValue(MinValueProperty);

		public static void SetMinValue(DependencyObject obj, int? value) =>
			obj.SetValue(MinValueProperty, value);

		private static void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
			e.Handled = !e.Text.All(char.IsDigit);

		private static void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Return)
				return;

			if (sender is not TextBox textBox)
				return;

			ValidateTextBoxValue(textBox);
			textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
		}

		private static void TextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (sender is not TextBox textBox)
				return;

			ValidateTextBoxValue(textBox);
		}

		private static void ValidateTextBoxValue(TextBox textBox)
		{
			if (int.TryParse(textBox.Text, out int currentValue))
			{
				int? maxValue = GetMaxValue(textBox);
				int? minValue = GetMinValue(textBox);

				if (maxValue.HasValue && currentValue > maxValue.Value)
					textBox.Text = maxValue.Value.ToString();
				else if (minValue.HasValue && currentValue < minValue.Value)
					textBox.Text = minValue.Value.ToString();
			}
			else
			{
				int? minValue = GetMinValue(textBox);
				if (minValue.HasValue)
					textBox.Text = minValue.Value.ToString();
			}
		}

		private static void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
		{
			if (sender is not TextBox)
				return;

			if (!e.SourceDataObject.GetDataPresent(DataFormats.Text))
			{
				e.CancelCommand();
				return;
			}

			var pastedText = e.SourceDataObject.GetData(DataFormats.Text) as string ?? string.Empty;
			if (!pastedText.All(char.IsDigit))
				e.CancelCommand();
		}

		private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is not TextBox textBox)
				return;

			textBox.PreviewTextInput -= TextBox_PreviewTextInput;
			textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
			textBox.LostFocus -= TextBox_LostFocus;
			DataObject.RemovePastingHandler(textBox, TextBox_Pasting);

			if (GetNumericOnly(textBox))
				textBox.PreviewTextInput += TextBox_PreviewTextInput;

			if (GetMinValue(textBox).HasValue || GetMaxValue(textBox).HasValue)
			{
				textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
				textBox.LostFocus += TextBox_LostFocus;
			}

			if (GetNumericOnly(textBox) || GetMinValue(textBox).HasValue || GetMaxValue(textBox).HasValue)
				DataObject.AddPastingHandler(textBox, TextBox_Pasting);
		}
	}
}
