// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	public partial class ExportSemanticDomainsDlg : Form
	{
		private FdoCache m_cache;
		public ExportSemanticDomainsDlg()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the dialog with all needed information.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Initialize( FdoCache cache)
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

		private object CreateItemForWs(CoreImpl.IWritingSystem xws)
		{
			return new Item() {Label = xws.DisplayLabel, Ws = xws.Handle};
		}

		public int SelectedWs
		{
			get { return ((Item) m_writingSystemsListBox.SelectedItem).Ws; }
		}

		public bool AllQuestions
		{
			get { return m_EnglishInRedCheckBox.Enabled && m_EnglishInRedCheckBox.Checked; }
		}

		class Item
		{
			public string Label;
			public int Ws;
			public override string ToString()
			{
				return Label;
			}
		}

		private void m_writingSystemsListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_EnglishInRedCheckBox.Enabled =
				SelectedWs != m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
		}
	}
}
