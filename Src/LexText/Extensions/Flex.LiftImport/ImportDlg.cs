using System;
using System.IO;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.LexText.Controls;
using XCore;
using SIL.Fieldworks.LexText;

namespace SIL.FieldWorks.LexText
{
	public partial class LiftImportDlg : Form, IFwExtension
	{
		private FdoCache _cache;
	 //   private Mediator _mediator;
		public LiftImportDlg()
		{
			InitializeComponent();
		}
		/// <summary>
		/// From IFwExtension
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		void IFwExtension.Init(FdoCache cache, Mediator mediator)
		{
			_cache = cache;
		  //  _mediator = mediator;
		}

		/// <summary>
		/// (IFwImportDialog)Shows the dialog as a modal dialog
		/// </summary>
		/// <returns>A DialogResult value</returns>
		public new DialogResult Show(IWin32Window owner)
		{
			return this.ShowDialog(owner);
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			UpdateButtons();
			if (!btnOK.Enabled)
				return;
			DoImport();
			this.Close();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void btnBrowse_Click(object sender, EventArgs e)
		{
			if (DialogResult.OK != openFileDialog1.ShowDialog())
				return;

			tbPath.Text = openFileDialog1.FileName;
			UpdateButtons();
		}

		private void UpdateButtons()
		{
			btnOK.Enabled = tbPath.Text.Length > 0 &&
				File.Exists(tbPath.Text);
		}

		private void DoImport()
		{
			_cache.BeginUndoTask(LiftImportStrings.ksUndoLIFTImport, LiftImportStrings.ksRedoLIFTImport);
			try
			{
				FlexLiftMerger flexImporter = new FlexLiftMerger(_cache);
				this.Cursor = Cursors.WaitCursor;
			//    flexImporter.ImportLiftFile(openFileDialog1.FileName);
			}
			//catch (Exception error)
			//{
			//    //TODO: shouldn't there be a method on the cache to cancel the undo task?
			//    MessageBox.Show("Something went wrong while FieldWorks was attempting to import.");
			//    //TODO: is it may be better to just let it die of the normal green box death?
			//}
			finally
			{
				_cache.EndUndoTask();
			}
		}

		private void WeSayImportDlg_Load(object sender, EventArgs e)
		{
			UpdateButtons();
		}
	}
}