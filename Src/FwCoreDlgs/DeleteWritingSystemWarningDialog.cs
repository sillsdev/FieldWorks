using System.Windows.Forms;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// A warning dialog that we can't quite do with MessageBox because custom text is required for the "Yes" button.
	/// </summary>
	public partial class DeleteWritingSystemWarningDialog : Form
	{
		/// <summary>
		/// Grrr....make one.
		/// </summary>
		public DeleteWritingSystemWarningDialog()
		{
			InitializeComponent();
			warningIconBox.BackgroundImageLayout = ImageLayout.Center;
			warningIconBox.BackgroundImage = System.Drawing.SystemIcons.Warning.ToBitmap();
		}

		internal void SetWsName(string name)
		{
			mainMessage.Text = string.Format(mainMessage.Text, name);
		}
	}
}
