using System;

namespace System.Windows.Forms
{
	/// <summary>
	/// A System.Windows.Forms wrapper for OpenFileDialog.
	/// </summary>
	public class OpenFileDialog : FileDialog
	{
		private Gtk.CheckButton	m_readOnly;
		private static OpenFileDialog ofd_;

		/// <summary>
		/// Constructor is private to allow a singleton.
		/// </summary>
		private OpenFileDialog() : base(Gtk.FileChooserAction.Open, "Open", Gtk.Stock.Open)
		{
			Console.WriteLine("OpenFileDialog ctor invoked");

			CheckFileExists = true;

			m_readOnly = new Gtk.CheckButton("Open file as _read-only");
			((Gtk.HBox) m_dialog.ExtraWidget).PackEnd(m_readOnly, false, false, 0);
			m_readOnly.Active = false;
			m_readOnly.Visible = false;

			m_dialog.SelectMultiple = false;

			m_dialog.SetResponseSensitive(Gtk.ResponseType.Ok, false);

			m_dialog.SelectionChanged += SelectionChangedHandler;
		}

		/// <summary>
		/// Get the singleton instance of the OpenFileDialog
		/// </summary>
		/// <returns>
		/// A <see cref="OpenFileDialog"/> the singleton instance of OpenFileDialog.
		/// </returns>
		public static OpenFileDialog GetInstance()
		{
			if (ofd_ == null)
				ofd_ = new OpenFileDialog();
			return ofd_;
		}

		/// <summary>
		/// Handler for when the selection of a file changes.
		/// </summary>
		private void SelectionChangedHandler(object sender, EventArgs e)
		{
			Console.WriteLine("OpenFileDialog.SelectionChangedHandler() invoked");

			m_dialog.SetResponseSensitive(Gtk.ResponseType.Ok, m_dialog.Filename != null);
		}

		/// <summary>
		/// Set the filename for this dialog.
		/// </summary>
		/// <param name="fileName">
		/// A <see cref="System.String"/> containing the filename to set.
		/// </param>
		protected override void SetFileName(string fileName)
		{
			Console.WriteLine("OpenFileDialog.SetFileName() invoked");

			// Note: Gtk.FileChooserDialog.SetFilename() requires a full path. Even then,
			//       it doesn't return false (as documented) when it can't select the
			//       named file, so its return value is of no real use to the caller :-(
			/*m_dialog.SetFilename(System.IO.Path.Combine(fileDetails.directoryName, 					fileDetails.fileName));*/
			//m_dialog.SetFilename(System.IO.Path.Combine(m_dialog.CurrentFolder,
				//fileName));
		}

		/// <value>
		/// Whether or not multiple files may be selected.
		/// </value>
		public bool Multiselect
		{
			get { return m_dialog.SelectMultiple; }
			set { m_dialog.SelectMultiple = value; }
		}

		/// <value>
		/// If the file is read-only or not.
		/// </value>
		public bool ReadOnlyChecked
		{
			get { return m_readOnly.Active; }
			set { m_readOnly.Active = value; }
		}

		/// <value>
		/// Show whether or not the file is read-only or not.
		/// </value>
		public bool ShowReadOnly
		{
			get { return m_readOnly.Visible; }
			set { m_readOnly.Visible = value; }
		}
	}
}
