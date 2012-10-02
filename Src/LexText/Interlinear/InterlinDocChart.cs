using System.Windows.Forms;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Dummy subclass so we can design the Chart tab.
	/// </summary>
	public class InterlinDocChart : UserControl
	{
		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// InterlinDocChart
			//
			this.AccessibleName = "InterlinDocChart";
			this.Name = "InterlinDocChart";
			this.ResumeLayout(false);
		}

		public InterlinDocChart()
		{
			InitializeComponent();
		}
	}
}