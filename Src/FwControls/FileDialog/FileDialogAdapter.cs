// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls.FileDialog
{
	/// <summary>
	/// File dialog adapter.
	/// </summary>
	public abstract class FileDialogAdapter : IFileDialog, IDisposable
	{
		/// <summary/>
		protected IFileDialog m_dlg;

		#region IFileDialog implementation
		/// <summary>
		/// Occurs when the component is disposed by a call to the Dispose method.
		/// </summary>
		public event EventHandler Disposed
		{
			add { m_dlg.Disposed += value; }
			remove { m_dlg.Disposed -= value; }
		}

		/// <summary>
		/// Occurs when the user clicks on the Open or Save button on a file dialog box.
		/// </summary>
		public event CancelEventHandler FileOk
		{
			add { m_dlg.FileOk += value; }
			remove { m_dlg.FileOk -= value; }
		}

		/// <summary>
		/// Occurs when the user clicks the Help button on a common dialog box.
		/// </summary>
		public event EventHandler HelpRequest
		{
			add { m_dlg.HelpRequest += value; }
			remove { m_dlg.HelpRequest -= value; }
		}

		/// <summary>
		/// Resets all properties to their default values.
		/// </summary>
		public void Reset()
		{
			m_dlg.Reset();
		}

		/// <summary>
		/// Runs the dialog box with a default owner.
		/// </summary>
		public DialogResult ShowDialog()
		{
			return m_dlg.ShowDialog();
		}

		/// <summary>
		/// Runs the dialog box with the specified owner.
		/// </summary>
		public DialogResult ShowDialog(IWin32Window owner)
		{
			return m_dlg.ShowDialog(owner);
		}

		/// <summary>
		/// Gets or sets a value indicating whether the dialog box automatically adds an extension
		/// to a file name if the user omits the extension.
		/// </summary>
		public bool AddExtension
		{
			get { return m_dlg.AddExtension; }
			set { m_dlg.AddExtension = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the dialog box displays a warning if the user
		/// specifies a file name that does not exist.
		/// </summary>
		public bool CheckFileExists
		{
			get { return m_dlg.CheckFileExists; }
			set { m_dlg.CheckFileExists = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the dialog box displays a warning if the user
		/// specifies a path that does not exist.
		/// </summary>
		public bool CheckPathExists
		{
			get { return m_dlg.CheckPathExists; }
			set { m_dlg.CheckPathExists = value; }
		}

		/// <summary>
		/// Gets or sets the default file name extension.
		/// </summary>
		public string DefaultExt
		{
			get { return m_dlg.DefaultExt; }
			set { m_dlg.DefaultExt = value; }
		}

		/// <summary>
		/// Gets or sets a string containing the file name selected in the file dialog box.
		/// </summary>
		/// <value>The name of the file.</value>
		public string FileName
		{
			get { return m_dlg.FileName; }
			set { m_dlg.FileName = value; }
		}

		/// <summary>
		/// Gets the file names of all selected files in the dialog box.
		/// </summary>
		/// <value>The file names.</value>
		public string[] FileNames
		{
			get { return m_dlg.FileNames; }
		}

		/// <summary>
		/// Gets or sets the current file name filter string, which determines the choices that
		/// appear in the "Save as file type" or "Files of type" box in the dialog box.
		/// </summary>
		/// <value>The filter.</value>
		public string Filter
		{
			get { return m_dlg.Filter; }
			set { m_dlg.Filter = value; }
		}

		/// <summary>
		/// Gets or sets the index of the filter currently selected in the file dialog box.
		/// </summary>
		/// <value>The index of the filter.</value>
		public int FilterIndex
		{
			get { return m_dlg.FilterIndex; }
			set { m_dlg.FilterIndex = value; }
		}

		/// <summary>
		/// Gets or sets the initial directory displayed by the file dialog box.
		/// </summary>
		/// <value>The initial directory.</value>
		public string InitialDirectory
		{
			get { return m_dlg.InitialDirectory; }
			set { m_dlg.InitialDirectory = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the dialog box restores the directory to the
		/// previously selected directory before closing.
		/// </summary>
		public bool RestoreDirectory
		{
			get { return m_dlg.RestoreDirectory; }
			set { m_dlg.RestoreDirectory = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the Help button is displayed in the file
		/// dialog box.
		/// </summary>
		public bool ShowHelp
		{
			get { return m_dlg.ShowHelp; }
			set { m_dlg.ShowHelp = value; }
		}

		/// <summary>
		/// Gets or sets whether the dialog box supports displaying and saving files that have
		/// multiple file name extensions.
		/// </summary>
		public bool SupportMultiDottedExtensions
		{
			get { return m_dlg.SupportMultiDottedExtensions; }
			set { m_dlg.SupportMultiDottedExtensions = value; }
		}

		/// <summary>
		/// Gets or sets the file dialog box title.
		/// </summary>
		/// <value>The title.</value>
		public string Title
		{
			get { return m_dlg.Title; }
			set { m_dlg.Title = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the dialog box accepts only valid Win32 file names.
		/// </summary>
		public bool ValidateNames
		{
			get { return m_dlg.ValidateNames; }
			set { m_dlg.ValidateNames = value; }
		}
		#endregion

		#region Disposable stuff
#if DEBUG
		/// <summary/>
		~FileDialogAdapter()
		{
			Dispose(false);
		}
#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + ". *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				var disposable = m_dlg as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}
			m_dlg = null;
			IsDisposed = true;
		}
		#endregion
	}
}
