// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TextFieldOptions.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>

using System;
using System.Windows.Forms;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;

namespace SIL.FieldWorks.LexText.Controls.DataNotebook
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class TextFieldOptions : UserControl
	{
		FdoCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;
		IVwStylesheet m_stylesheet;
		string m_sValidShortLim;
		bool m_fHandlingTextChanged = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="TextFieldOptions"/> class.
		/// </summary>
		public TextFieldOptions()
		{
			InitializeComponent();
		}

		private void m_chkAfterShortLine_CheckedChanged(object sender, EventArgs e)
		{
			m_tbShortLength.Enabled = m_chkAfterShortLine.Checked;
		}

		private void m_btnStyles_Click(object sender, EventArgs e)
		{
			//WANTPORT  FWR-2846
			MessageBox.Show(this, "This is not yet implemented.", "Please be patient");
		}

		internal void Initialize(FdoCache cache, IHelpTopicProvider helpTopicProvider, IApp app, IVwStylesheet stylesheet,
			NotebookImportWiz.RnSfMarker rsfm)
		{
			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			m_stylesheet = stylesheet;
			m_chkForEachLine.Checked = rsfm.m_txo.m_fStartParaNewLine;
			m_chkAfterBlankLine.Checked = rsfm.m_txo.m_fStartParaBlankLine;
			m_chkWhenIndented.Checked = rsfm.m_txo.m_fStartParaIndented;
			m_chkAfterShortLine.Checked = rsfm.m_txo.m_fStartParaShortLine;
			m_tbShortLength.Text = rsfm.m_txo.m_cchShortLim.ToString();
			m_tbShortLength.Enabled = rsfm.m_txo.m_fStartParaShortLine;

			m_btnAddWritingSystem.Initialize(m_cache, helpTopicProvider, app, stylesheet);
			NotebookImportWiz.InitializeWritingSystemCombo(rsfm.m_txo.m_wsId, m_cache,
				m_cbWritingSystem);
			InitializeStylesCombo(rsfm.m_txo.m_sStyle);
		}

		private void InitializeStylesCombo(string sStyle)
		{
			m_cbStyles.Items.Clear();
			m_cbStyles.Sorted = true;
			for (int i = 0; i < m_stylesheet.CStyles; ++i)
			{
				int hvo = m_stylesheet.get_NthStyle(i);
				IStStyle sty = m_cache.ServiceLocator.GetInstance<IStStyleRepository>().GetObject(hvo);
				if (sty.Type == StyleType.kstParagraph)
					m_cbStyles.Items.Add(sty);
			}
			foreach (IStStyle sty in m_cbStyles.Items)
			{
				if (sty.Name == sStyle)
				{
					m_cbStyles.SelectedItem = sty;
					break;
				}
			}
		}

		public bool ParaForEachLine
		{
			get { return m_chkForEachLine.Checked; }
		}

		public bool ParaAfterBlankLine
		{
			get { return m_chkAfterBlankLine.Checked; }
		}

		public bool ParaWhenIndented
		{
			get { return m_chkWhenIndented.Checked; }
		}

		public bool ParaAfterShortLine
		{
			get { return m_chkAfterShortLine.Checked; }
		}

		public int ShortLineLimit
		{
			get
			{
				if (String.IsNullOrEmpty(m_tbShortLength.Text))
				{
					return 0;
				}
				else
				{
					int cch;
					if (Int32.TryParse(m_tbShortLength.Text, out cch))
						return cch;
					else
						return 0;
				}
			}
		}

		public string WritingSystem
		{
			get
			{
				var ws = m_cbWritingSystem.SelectedItem as WritingSystem;
				if (ws != null)
					return ws.Id;
				else
					return null;
			}
		}

		public string Style
		{
			get
			{
				IStStyle style = m_cbStyles.SelectedItem as IStStyle;
				if (style != null)
					return style.Name;
				else
					return null;
			}
		}

		/// <summary>
		/// Validate user input to allow only decimal digits that can be parsed into an integer.
		/// </summary>
		private void m_tbShortLength_TextChanged(object sender, EventArgs e)
		{
			if (m_fHandlingTextChanged)
				return;
			try
			{
				m_fHandlingTextChanged = true;
				if (String.IsNullOrEmpty(m_tbShortLength.Text))
				{
					m_sValidShortLim = m_tbShortLength.Text;
				}
				else
				{
					int cch;
					if (Int32.TryParse(m_tbShortLength.Text, out cch))
					{
						m_sValidShortLim = m_tbShortLength.Text;
					}
					else
					{
						int ichSel = m_tbShortLength.SelectionStart;
						m_tbShortLength.Text = m_sValidShortLim;
						if (ichSel > 0)
							m_tbShortLength.SelectionStart = ichSel - 1;
					}
				}
			}
			finally
			{
				m_fHandlingTextChanged = false;
			}
		}

		private void m_btnAddWritingSystem_WritingSystemAdded(object sender, EventArgs e)
		{
			WritingSystem ws = m_btnAddWritingSystem.NewWritingSystem;
			if (ws != null)
				NotebookImportWiz.InitializeWritingSystemCombo(ws.Id, m_cache,
					m_cbWritingSystem);
		}
	}
}
