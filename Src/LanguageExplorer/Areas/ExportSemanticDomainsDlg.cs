// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;

namespace LanguageExplorer.Areas
{
	public partial class ExportSemanticDomainsDlg : Form
	{
		private LcmCache m_cache;
		public ExportSemanticDomainsDlg()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Initialize the dialog with all needed information.
		/// </summary>
		public void Initialize(LcmCache cache)
		{
			m_cache = cache;
			FillInWritingSystems();
		}

		private void FillInWritingSystems()
		{
			foreach (var xws in m_cache.LangProject.AnalysisWritingSystems)
			{
				m_writingSystemsListBox.Items.Add(CreateItemForWs(xws));
			}
			foreach (var xws in m_cache.LangProject.VernacularWritingSystems)
			{
				m_writingSystemsListBox.Items.Add(CreateItemForWs(xws));
			}
			m_writingSystemsListBox.SelectedIndex = 0;
		}

		private static object CreateItemForWs(CoreWritingSystemDefinition xws)
		{
			return new Item
			{
				Label = xws.DisplayLabel,
				Ws = xws.Handle
			};
		}

		public int SelectedWs => ((Item)m_writingSystemsListBox.SelectedItem).Ws;

		public bool AllQuestions => m_EnglishInRedCheckBox.Enabled && m_EnglishInRedCheckBox.Checked;

		private void m_writingSystemsListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_EnglishInRedCheckBox.Enabled =
				SelectedWs != m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
		}

		private sealed class Item
		{
			public string Label;
			public int Ws;
			public override string ToString()
			{
				return Label;
			}
		}
	}
}