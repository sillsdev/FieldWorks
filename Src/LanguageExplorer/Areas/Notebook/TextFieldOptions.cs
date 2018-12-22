// Copyright (c) 2010-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.WritingSystems;

namespace LanguageExplorer.Areas.Notebook
{
	/// <summary />
	public partial class TextFieldOptions : UserControl
	{
		private LcmCache m_cache;
		private IVwStylesheet m_stylesheet;
		private string m_sValidShortLim;
		private bool m_fHandlingTextChanged;

		/// <summary />
		public TextFieldOptions()
		{
			InitializeComponent();
		}

		private void m_chkAfterShortLine_CheckedChanged(object sender, EventArgs e)
		{
			m_tbShortLength.Enabled = m_chkAfterShortLine.Checked;
		}

		internal void Initialize(LcmCache cache, IHelpTopicProvider helpTopicProvider, IApp app, IVwStylesheet stylesheet, RnSfMarker rsfm)
		{
			m_cache = cache;
			m_stylesheet = stylesheet;
			m_chkForEachLine.Checked = rsfm.m_txo.m_fStartParaNewLine;
			m_chkAfterBlankLine.Checked = rsfm.m_txo.m_fStartParaBlankLine;
			m_chkWhenIndented.Checked = rsfm.m_txo.m_fStartParaIndented;
			m_chkAfterShortLine.Checked = rsfm.m_txo.m_fStartParaShortLine;
			m_tbShortLength.Text = rsfm.m_txo.m_cchShortLim.ToString();
			m_tbShortLength.Enabled = rsfm.m_txo.m_fStartParaShortLine;
			m_btnAddWritingSystem.Initialize(m_cache, helpTopicProvider, app);
			m_cbWritingSystem.InitializeWritingSystemCombo(m_cache, rsfm.m_txo.m_wsId);
			InitializeStylesCombo(rsfm.m_txo.m_sStyle);
		}

		private void InitializeStylesCombo(string sStyle)
		{
			m_cbStyles.Items.Clear();
			m_cbStyles.Sorted = true;
			for (var i = 0; i < m_stylesheet.CStyles; ++i)
			{
				var hvo = m_stylesheet.get_NthStyle(i);
				var sty = m_cache.ServiceLocator.GetInstance<IStStyleRepository>().GetObject(hvo);
				if (sty.Type == StyleType.kstParagraph)
				{
					m_cbStyles.Items.Add(sty);
				}
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

		public bool ParaForEachLine => m_chkForEachLine.Checked;

		public bool ParaAfterBlankLine => m_chkAfterBlankLine.Checked;

		public bool ParaWhenIndented => m_chkWhenIndented.Checked;

		public bool ParaAfterShortLine => m_chkAfterShortLine.Checked;

		public int ShortLineLimit
		{
			get
			{
				if (string.IsNullOrEmpty(m_tbShortLength.Text))
				{
					return 0;
				}
				int cch;
				return int.TryParse(m_tbShortLength.Text, out cch) ? cch : 0;
			}
		}

		public string WritingSystem => (m_cbWritingSystem.SelectedItem as CoreWritingSystemDefinition)?.Id;

		public string Style => (m_cbStyles.SelectedItem as IStStyle)?.Name;

		/// <summary>
		/// Validate user input to allow only decimal digits that can be parsed into an integer.
		/// </summary>
		private void m_tbShortLength_TextChanged(object sender, EventArgs e)
		{
			if (m_fHandlingTextChanged)
			{
				return;
			}
			try
			{
				m_fHandlingTextChanged = true;
				if (string.IsNullOrEmpty(m_tbShortLength.Text))
				{
					m_sValidShortLim = m_tbShortLength.Text;
				}
				else
				{
					int cch;
					if (int.TryParse(m_tbShortLength.Text, out cch))
					{
						m_sValidShortLim = m_tbShortLength.Text;
					}
					else
					{
						var ichSel = m_tbShortLength.SelectionStart;
						m_tbShortLength.Text = m_sValidShortLim;
						if (ichSel > 0)
						{
							m_tbShortLength.SelectionStart = ichSel - 1;
						}
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
			var ws = m_btnAddWritingSystem.NewWritingSystem;
			if (ws != null)
			{
				m_cbWritingSystem.InitializeWritingSystemCombo(m_cache, ws.Id);
			}
		}
	}
}