using System;
using System.Globalization;
using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks.DictionaryDetailsView
{
    /// <summary>
    /// This view is responsible for the display of options for a PictureNode in the configuration dialog
    /// </summary>
    public partial class PictureOptionsView : UserControl
    {
        public PictureOptionsView()
        {
            InitializeComponent();
        }

        public AlignmentType Alignment
        {
            get => (AlignmentType)alignmentComboBox.SelectedItem;
			set => alignmentComboBox.SelectedItem = value;
		}

        public double PictureWidth
		{
			get => double.TryParse(widthTextBox.Text, NumberStyles.Float, null, out var width)
				? Math.Round(width, 2)
				: 0.0f;
			set => widthTextBox.Text = value.ToString("F2");
		}

        public event EventHandler AlignmentChanged
        {
            add => alignmentComboBox.SelectedIndexChanged += value;
			remove => alignmentComboBox.SelectedIndexChanged -= value;
		}

        public event EventHandler WidthChanged
        {
            add => widthTextBox.TextChanged += value;
			remove => widthTextBox.TextChanged -= value;
		}
    }
}
