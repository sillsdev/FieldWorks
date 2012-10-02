// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeExportSaveFileDialog.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.FDO;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.TE
{
	/// <summary>Indicates the type of export</summary>
	public enum FileType
	{
		/// <summary>USFM (Toolbox)</summary>
		ToolBox,
		/// <summary>USFM (Paratext)</summary>
		Paratext,
		/// <summary>Open XML for Editing Scripture</summary>
		OXES,
		/// <summary>XHTML</summary>
		XHTML,
		/// <summary>Open Document Type</summary>
		ODT,
		/// <summary>Portable Document Format</summary>
		PDF,
		/// <summary>Rich Text Format</summary>
		RTF,
		/// <summary>Open XML for Exchanging Scripture Annotations</summary>
		OXESA,
		/// <summary>Open XML for Exchanging Key Terms</summary>
		OXEKT,
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TeImportExportFileDialog wraps the FileDialog for TE Import and Export dialogs. Takes
	/// care of setting all the standard properties, some of which are based on the file type.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeImportExportFileDialog : IDisposable
	{
		#region Member variables
		private readonly string m_dbName;
		private readonly FileType m_fileType;
		private FileDialog m_dlg;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeImportExportFileDialog"/> class.
		/// </summary>
		/// <param name="dbName">Name of the database.</param>
		/// <param name="fileType">Type of the file to import or export.</param>
		/// ------------------------------------------------------------------------------------
		public TeImportExportFileDialog(string dbName, FileType fileType)
		{
			m_dbName = dbName;
			m_fileType = fileType;
		}
		#endregion

		#region IDisposable Members
#if DEBUG
		/// <summary/>
		~TeImportExportFileDialog()
		{
			Dispose(false);
		}
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing)
			{
				if (m_dlg != null)
					m_dlg.Dispose();
			}
			m_dlg = null;
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the drive, folder and filename for the FileDialog and then show it.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <param name="owner">The owning window.</param>
		/// ------------------------------------------------------------------------------------
		public DialogResult ShowSaveDialog(string filename, IWin32Window owner)
		{
			return ShowSaveDialog(filename, true, owner);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the drive, folder and filename for the FileDialog and then show it.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <param name="fOverwritePrompt">Value indicating whether the Save As dialog box
		/// displays a warning if the user spaecifies a file name that already exists.</param>
		/// <param name="owner">The owning window.</param>
		/// <returns>The dialog result from the SaveFileDialog</returns>
		/// ------------------------------------------------------------------------------------
		public DialogResult ShowSaveDialog(string filename, bool fOverwritePrompt,
			IWin32Window owner)
		{
			bool fFirstTimeInit = (m_dlg == null);

			if (fFirstTimeInit)
			{
				m_dlg = new SaveFileDialog();
				((SaveFileDialog)m_dlg).OverwritePrompt = fOverwritePrompt;
			}

			return InitAndShowDialog(fFirstTimeInit, filename, owner);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the open dialog.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <param name="owner">The owning window.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public DialogResult ShowOpenDialog(string filename, IWin32Window owner)
		{
			bool fFirstTimeInit = (m_dlg == null);

			if (fFirstTimeInit)
				m_dlg = new OpenFileDialog();

			return InitAndShowDialog(fFirstTimeInit, filename, owner);
		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the and show dialog.
		/// </summary>
		/// <param name="fFirstTimeInit">if set to <c>true</c> it is the first time to initialize
		/// the dialog.</param>
		/// <param name="fileName">Name of the file from which we will import or to which we
		/// will export.</param>
		/// <param name="owner">The owning window.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private DialogResult InitAndShowDialog(bool fFirstTimeInit, string fileName,
			IWin32Window owner)
		{
			if (fFirstTimeInit)
			{
				m_dlg.AddExtension = true;
				m_dlg.CheckPathExists = true;
				m_dlg.DefaultExt = DefaultExtension;
				m_dlg.Filter = FileExtensionFilterString;
				m_dlg.FilterIndex = 0;
			}

			string sPathName = fileName;
			try
			{
				Path.GetFullPath(sPathName);
			}
			catch
			{
				sPathName = DefaultFilePath;
			}

			m_dlg.FileName = Path.GetFileName(sPathName);
			m_dlg.InitialDirectory = Path.GetDirectoryName(sPathName);
			// if an initial directory cannot be determined, attempt to determine the root path.
			if (m_dlg.InitialDirectory == string.Empty)
			{
				string rootDriveName = Path.GetPathRoot(sPathName);
				if (rootDriveName != null)
					m_dlg.InitialDirectory = rootDriveName;
			}

			return m_dlg.ShowDialog(owner);
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the filename selected in the file save dialog box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string FileName
		{
			get { return m_dlg == null ? null : m_dlg.FileName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default output file path.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string DefaultFilePath
		{
			get
			{
				return Path.Combine(DefaultFolder, DefaultFileName);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default output folder path.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string DefaultFolder
		{
			get { return System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default output filename.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string DefaultFileName
		{
			get
			{
				string fileName = m_dbName;
				fileName += DefaultExtension;
				return fileName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default file extension.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string DefaultExtension
		{
			get
			{
				switch (m_fileType)
				{
					case FileType.ToolBox:
					case FileType.Paratext:
					default:
						return ".sf";
					case FileType.OXES:
						return FwFileExtensions.ksOpenXmlForEditingScripture;
					case FileType.OXEKT:
						return FwFileExtensions.ksOpenXmlForExchangingKeyTerms;
					case FileType.XHTML:
						return ".xhtml";
					case FileType.RTF:
						return ".rtf";
					case FileType.ODT:
						return ".odt";
					case FileType.PDF:
						return ".pdf";
					case FileType.OXESA:
						return FwFileExtensions.ksOpenXmlForExchangingScrAnnotations;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the file extension filter string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string FileExtensionFilterString
		{
			get
			{
				List<FileFilterType> fileTypes = new List<FileFilterType>(2);

				switch (m_fileType)
				{
					case FileType.ToolBox:
					case FileType.Paratext:
					default:
						fileTypes.Add(FileFilterType.DefaultStandardFormat); break;
					case FileType.OXES:
						fileTypes.Add(FileFilterType.OXES);
						fileTypes.Add(FileFilterType.XML);
						break;
					case FileType.XHTML:
						fileTypes.Add(FileFilterType.XHTML);
						fileTypes.Add(FileFilterType.HTML);
						break;
					case FileType.RTF:
						fileTypes.Add(FileFilterType.RichTextFormat); break;
					case FileType.ODT:
						fileTypes.Add(FileFilterType.OpenOffice); break;
					case FileType.PDF:
						fileTypes.Add(FileFilterType.PDF); break;
					case FileType.OXESA:
						fileTypes.Add(FileFilterType.OXESA);
						fileTypes.Add(FileFilterType.XML);
						break;
					case FileType.OXEKT:
						fileTypes.Add(FileFilterType.OXEKT);
						fileTypes.Add(FileFilterType.XML);
						break;
				}
				fileTypes.Add(FileFilterType.AllFiles);
				return ResourceHelper.BuildFileFilter(fileTypes);
			}
		}
		#endregion
	}
}
