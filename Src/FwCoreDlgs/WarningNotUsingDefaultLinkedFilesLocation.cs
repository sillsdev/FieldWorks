using System;
using System.Windows.Forms;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Warn the user when they choose a custom location for linked files that Send/Receive will not send these files.
	/// </summary>
	public partial class WarningNotUsingDefaultLinkedFilesLocation : Form
	{
		/// <summary>
		/// Warn the user when they choose a custom location for linked files that Send/Receive will not send these files.
		/// </summary>
		public WarningNotUsingDefaultLinkedFilesLocation()
		{
			InitializeComponent();
		}

		private void btn_help_Click(object sender, EventArgs e)
		{

		}
	}
}
