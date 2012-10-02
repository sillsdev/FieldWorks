// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MultiLingualFieldOptions.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.LexText.Controls.DataNotebook
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class StringFieldOptions : UserControl
	{
		FdoCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StringFieldOptions"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringFieldOptions()
		{
			InitializeComponent();
		}

		internal void Initialize(FdoCache cache, IHelpTopicProvider helpTopicProvider, IApp app, IVwStylesheet stylesheet,
			NotebookImportWiz.RnSfMarker rsfm)
		{
			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			m_btnAddWritingSystem.Initialize(cache, helpTopicProvider, app, stylesheet);
			NotebookImportWiz.InitializeWritingSystemCombo(rsfm.m_sto.m_wsId, cache,
				m_cbWritingSystem);
		}

		public string WritingSystem
		{
			get
			{
				var ws = m_cbWritingSystem.SelectedItem as IWritingSystem;
				if (ws == null)
					return null;
				else
					return ws.Id;
			}
		}

		private void m_btnAddWritingSystem_WritingSystemAdded(object sender, EventArgs e)
		{
			IWritingSystem ws = m_btnAddWritingSystem.NewWritingSystem;
			if (ws != null)
				NotebookImportWiz.InitializeWritingSystemCombo(ws.Id, m_cache,
					m_cbWritingSystem);
		}
	}
}
