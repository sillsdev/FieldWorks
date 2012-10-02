// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ResolveKeyTermRenderingImportConflictDlg.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Windows.Forms;
using SIL.Utils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ResolveKeyTermRenderingImportConflictDlg : Form
	{
		private IWin32Window m_owner;
		private FwLabel m_verseTextLabel;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ResolveKeyTermRenderingImportConflictDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ResolveKeyTermRenderingImportConflictDlg(IWin32Window owner, IChkRef occurrence,
			string existingRendering, string importedRendering, IVwStylesheet ss) : this()
		{
			FdoCache cache = occurrence.Cache;
			IScripture scr = cache.LangProject.TranslatedScriptureOA;
			ScrReference scrRef = (new ScrReference(occurrence.Ref, scr.Versification));
			m_owner = owner;
			m_lblAnalysis.Text = occurrence.OwnerOfClass<IChkTerm>().Name.AnalysisDefaultWritingSystem.Text;
			m_lblOriginal.Text = occurrence.KeyWord.Text;
			m_lblScrReference.Text = scrRef.ToString();
			m_btnExisting.Text = String.Format(m_btnExisting.Text, existingRendering);
			m_btnImported.Text = String.Format(m_btnImported.Text, importedRendering);

			// We do this outside the designer-controlled code because it does funny things
			// to FwMultiParaTextBox, owing to the need for a writing system factory, and some
			// properties it should not persist but I can't persuade it not to.
//			IStText text = new NonEditableMultiTss(TeEditingHelper.GetVerseText(cache.LangProject.TranslatedScriptureOA, scrRef).ToString(true, " "));
			//m_verseTextLabel = new FwMultiParaTextBox(text, ss);
			m_verseTextLabel = new FwLabel();
			m_verseTextLabel.WritingSystemFactory = cache.WritingSystemFactory; // set ASAP.
			m_verseTextLabel.WritingSystemCode = cache.DefaultVernWs;
			m_verseTextLabel.StyleSheet = ss; // before setting text, otherwise it gets confused about height needed.
			m_verseTextLabel.Location = new Point(0, 0);
			m_verseTextLabel.Name = "m_textBox";
			m_verseTextLabel.Dock = DockStyle.Fill;
			m_verseTextLabel.TabIndex = 0;
			m_verseTextLabel.TextAlign = ContentAlignment.TopLeft;
			//m_verseTextLabel.BorderStyle = BorderStyle.None;
			//m_textBox.SuppressEnter = true;
			m_pnlActualVerseText.Controls.Add(m_verseTextLabel);
			// ENHANCE: Figure out how to get each part (paragraph) of the verse onm its own line. Using newlines or hard line breaks doesn't work.
			ITsIncStrBldr bldr = TsIncStrBldrClass.Create();
			foreach (TeEditingHelper.VerseTextSubstring verseTextSubstring in TeEditingHelper.GetVerseText(cache.LangProject.TranslatedScriptureOA, scrRef))
			{
				bldr.AppendTsString(verseTextSubstring.Tss);
				bldr.Append(StringUtils.kChHardLB.ToString());
			}
			m_verseTextLabel.Tss = bldr.GetString();
			//m_verseTextLabel.Tss = TeEditingHelper.GetVerseText(cache.LangProject.TranslatedScriptureOA, scrRef).ToString(true, StringUtils.kChHardLB.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ResolveKeyTermRenderingImportConflictDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ResolveKeyTermRenderingImportConflictDlg()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to use the imported rendering.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool UseImportedRendering
		{
			get
			{
				return (ShowDialog(m_owner) == DialogResult.Yes);
			}
		}
	}
}