using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks
{
	public partial class DictionaryConfigurationManagerDlg : Form
	{
		public DictionaryConfigurationManagerDlg()
		{
			InitializeComponent();
		}

		private void ConfigurationsListViewKeyUp(object sender, KeyEventArgs e)
		{
			// Match Windows Explorer behaviour: allow renaming from the keyboard by pressing F2 or through the
			// "context menu" (since there is no context menu, go straight to rename from the "Application" key)
			if ((e.KeyCode == Keys.F2 || e.KeyCode == Keys.Apps) && configurationsListView.SelectedItems.Count == 1)
				configurationsListView.SelectedItems[0].BeginEdit();
		}
	}
}
