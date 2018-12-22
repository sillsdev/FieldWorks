// Copyright (c) 2010-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;

namespace LanguageExplorer.Areas.Notebook
{
	/// <summary />
	public partial class StringFieldOptions : UserControl
	{
		private LcmCache m_cache;

		/// <summary />
		public StringFieldOptions()
		{
			InitializeComponent();
		}

		internal void Initialize(LcmCache cache, IHelpTopicProvider helpTopicProvider, IApp app, RnSfMarker rsfm)
		{
			m_cache = cache;
			m_btnAddWritingSystem.Initialize(cache, helpTopicProvider, app);
			m_cbWritingSystem.InitializeWritingSystemCombo(cache, rsfm.m_sto.m_wsId);
		}

		public string WritingSystem => (m_cbWritingSystem.SelectedItem as CoreWritingSystemDefinition)?.Id;

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
