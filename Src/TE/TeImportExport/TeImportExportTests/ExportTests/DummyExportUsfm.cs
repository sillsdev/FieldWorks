// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DummyExportUsfm.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.TE.ExportTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// dummy class for ExportUsfm for testing
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyExportUsfm : ExportUsfm
	{
		/// <summary></summary>
		public bool m_fReadUsfmStyFile = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyExportUsfm(FdoCache cache)
			: this(cache, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyExportUsfm(FdoCache cache, FilteredScrBooks filter) :
			base(cache, filter, string.Empty, null)
		{
			if (m_file != null)
				m_file.Close();
			m_file = new DummyFileWriter();
			// Usually this is set by the main window and most tests need this.
			m_requestedAnalWS = new int[] { m_defaultAnalWS };
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the dummy file writer for the exported file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyFileWriter FileWriter
		{
			get
			{
				CheckDisposed();
				return (DummyFileWriter)m_file;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the dummy file writer for the STY file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyFileWriter FileWriterSty
		{
			get
			{
				CheckDisposed();
				return (DummyFileWriter)m_fileSty;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the base class member m_pictureFilesToCopy, which is a list of picture files
		/// to copy.
		/// </summary>
		/// <value>The picture files to copy.</value>
		/// ------------------------------------------------------------------------------------
		public List<string> PictureFilesToCopy
		{
			get
			{
				CheckDisposed();
				return m_pictureFilesToCopy;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Access the ParseBackTranslationVerses method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new List<ChapterVerseInfo>[] ParseBackTranslationVerses(ICmTranslation cmTrans)
		{
			CheckDisposed();

			return base.ParseBackTranslationVerses(cmTrans);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expose the ExportParagraph method for testing
		/// </summary>
		/// <param name="para"></param>
		/// ------------------------------------------------------------------------------------
		public new void ExportParagraph(IStTxtPara para)
		{
			CheckDisposed();

			InitializeStateVariables();
			CreateStyleTables();
			base.CreateAnnotationList(m_currentBookOrd);
			base.m_currentParaIsHeading = false;
			base.ExportParagraph(para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the ExportSection method for testing
		/// </summary>
		/// <param name="section">The section.</param>
		/// <param name="outputImplicitChapter">if set to <c>true</c> [output implicit chapter].</param>
		/// ------------------------------------------------------------------------------------
		public void ExportSection(IScrSection section, bool outputImplicitChapter)
		{
			CheckDisposed();

			CreateStyleTables();
			m_nonInterleavedBtWs = m_requestedAnalWS[0];
			base.CreateAnnotationList(m_currentBookOrd);
			using (ProgressDialogWithTask dlg = new ProgressDialogWithTask(Form.ActiveForm))
			{
				base.ExportSection(dlg, section, outputImplicitChapter);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expose the ExportBook method
		/// </summary>
		/// <param name="book"></param>
		/// ------------------------------------------------------------------------------------
		public void ExportBook(IScrBook book)
		{
			CheckDisposed();

			CreateStyleTables();
			m_nonInterleavedBtWs = m_requestedAnalWS[0];
			base.CreateAnnotationList(m_currentBookOrd);
			using (ProgressDialogWithTask dlg = new ProgressDialogWithTask(Form.ActiveForm))
			{
				base.ExportBook(dlg, book);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the file. This is virtual so it can be replaced in testing with an
		/// in-memory file writer.
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override FileWriter OpenFile(string fileName)
		{
			DummyFileWriter writer = new DummyFileWriter();
			writer.Open(fileName);
			return writer;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reads the usfm sty file. This is virtual so it can be conditional in testing because
		/// some tests expect to start with an empty list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ReadUsfmStyFile()
		{
			if (m_fReadUsfmStyFile)
				CallReadUsfmStyFile();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the ReadUsfmStyFile() method after first ensuring that the style table has
		/// been created
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CallReadUsfmStyFile()
		{
			CheckDisposed();

			if (m_UsfmStyFileAccessor == null)
				CreateStyleTables();
			base.ReadUsfmStyFile();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the GetExportBookCanonicalNum method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new int GetExportBookCanonicalNum(int bookNum, MarkupType markupSystem)
		{
			CheckDisposed();

			return ExportUsfm.GetExportBookCanonicalNum(bookNum, markupSystem);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the m_UsfmStyFileAccessor member.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public UsfmStyFileAccessor UsfmStyFileAccessor
		{
			get
			{
				CheckDisposed();
				return m_UsfmStyFileAccessor;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the m_ParatextSsfFileAccessor member.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ParatextSsfFileAccessor ParatextSsfFileAccessor
		{
			get
			{
				CheckDisposed();
				return m_ParatextSsfFileAccessor;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the method to update a Paratext ssf file.
		/// </summary>
		/// <param name="ssf">Stream for Paratext SSF that already exists.</param>
		/// <param name="format">the prefix, scheme, suffix, extension</param>
		/// <param name="sShortName">The short name of the Paratext project</param>
		/// <param name="styleSheetFile">The style sheet file.</param>
		/// <param name="projPath">The path where the project is located</param>
		/// <param name="ws">The HVO of the writing system for the current export.</param>
		/// ------------------------------------------------------------------------------------
		public XmlDocument UpdateSsfFile(TextReader ssf, FileNameFormat format, string sShortName,
			string styleSheetFile, string projPath, int ws)
		{
			CheckDisposed();

			return m_ParatextSsfFileAccessor.UpdateSsfFile(ssf, format, sShortName,
				styleSheetFile, projPath, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the style mapping.
		/// </summary>
		/// <param name="styleName">Name of the style.</param>
		/// <param name="marker">The marker.</param>
		/// ------------------------------------------------------------------------------------
		public void SetStyleMapping(string styleName, string marker)
		{
			CheckDisposed();

			if (m_UsfmStyFileAccessor == null)
				CreateStyleTables();
			base.SetStyleMapping(styleName, null, marker);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the paratext project files. (Creates and loads the style table first if
		/// this hasn't already been done -- production code does this in the Run method).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new void CreateParatextProjectFiles()
		{
			CheckDisposed();

			ParatextProjectShortName = "ABC";
			string outputFolder = Path.GetTempPath();
			ParatextProjectFolder = outputFolder;
			ReflectionHelper.SetField(this, "m_outputSpec", outputFolder);
			if (m_UsfmStyFileAccessor == null)
				CreateStyleTables();
			base.CreateParatextProjectFiles();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the CreateStyleTables() method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CallCreateStyleTables()
		{
			CheckDisposed();

			base.CreateStyleTables();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the VerseNumParse method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new void VerseNumParse(string sVerseNumber, out int beginVerse, out int endVerse,
			out bool invalidVerse)
		{
			CheckDisposed();

			base.VerseNumParse(sVerseNumber, out beginVerse, out endVerse, out invalidVerse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the ChapterNumStringToInt method.
		/// </summary>
		/// <param name="sNumber">The s number.</param>
		/// <param name="invalidChapter">if set to <c>true</c> [invalid chapter].</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public new int ChapterNumStringToInt(string sNumber, out bool invalidChapter)
		{
			CheckDisposed();

			return base.ChapterNumStringToInt(sNumber, out invalidChapter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up the dummy exporter for data in the given book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetContext(IScrBook book)
		{
			CheckDisposed();

			m_currentBookCode = book.BookId;
			m_currentBookOrd = book.CanonicalNum;
		}
	}
}
