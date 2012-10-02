using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The class for the overwrite existing files dialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FilesOverwriteDialog : Form, IFWDisposable
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FilesOverwriteDialog"/> class.
		/// </summary>
		/// <param name="filename">name of the file that may be overwritten</param>
		/// <param name="applicationName">Name of the application.</param>
		/// ------------------------------------------------------------------------------------
		public FilesOverwriteDialog(string filename, string applicationName)
		{
			InitializeComponent();

			Text = applicationName;
			lblFilename.Text = filename;
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the Yes button.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnYes_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Yes;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the YesToAll button.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnYesToAll_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the No button.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnNo_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.No;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the NoToAll button.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnNoToAll_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
		}
	}
}