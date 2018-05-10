// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary />
	public partial class AddWritingSystemButton : Button
	{
		LcmCache m_cache;
		private HashSet<string> m_existingWsIds;
		public event EventHandler WritingSystemAdded;
		private IHelpTopicProvider m_helpTopicProvider;
		private IApp m_app;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddWritingSystemButton"/> class.
		/// </summary>
		public AddWritingSystemButton()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public AddWritingSystemButton(IContainer container)
		{
			container.Add(this);
			InitializeComponent();
		}

		/// <summary>
		/// Initialize for adding new writing systems during import.
		/// </summary>
		public void Initialize(LcmCache cache, IHelpTopicProvider helpTopicProvider, IApp app, IEnumerable<CoreWritingSystemDefinition> existingWritingSystems)
		{
			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			m_existingWsIds = new HashSet<string>(existingWritingSystems.Select(ws => ws.Id).ToList());
		}

		/// <summary>
		/// Initialize for adding new writing systems.
		/// </summary>
		internal void Initialize(LcmCache cache, IHelpTopicProvider helpTopicProvider, IApp app)
		{
			Initialize(cache, helpTopicProvider, m_app, cache.ServiceLocator.WritingSystems.AllWritingSystems);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			// add the 'drop down' arrow to the text
			using (var p = new Pen(SystemColors.ControlText, 1))
			{
				var x = Width - 14; // 7 wide at top, and 7 in from right boundry
				var y = Height / 2 - 2; // up 2 past the mid point
				// 4 lines: len 7, len 5, len 3 and len 1
				e.Graphics.DrawLine(p, x, y, x + 7, y);
				e.Graphics.DrawLine(p, x + 1, y + 1, x + 1 + 5, y + 1);
				e.Graphics.DrawLine(p, x + 2, y + 2, x + 2 + 3, y + 2);
				e.Graphics.DrawLine(p, x + 3, y + 3, x + 3 + 1, y + 3);
			}
		}

		protected override void OnClick(EventArgs e)
		{
			base.OnClick(e);

			NewWritingSystem = null;

			// show the menu to select which type of writing system to create
			var mnuAddWs = components.ContextMenu("mnuAddWs");

			// look like the "Add" button on the WS properties dlg
			var xmlWs = GetOtherWritingSystems();
			var xmlWsV = new MenuItem[xmlWs.Count + 2]; // one for Vernacular
			var xmlWsA = new MenuItem[xmlWs.Count + 2]; // one for Analysis
			for (var i = 0; i < xmlWs.Count; i++)
			{
				var ws = xmlWs[i];
				xmlWsV[i] = new MenuItem(ws.DisplayLabel, mnuAddWS_Vern);
				xmlWsA[i] = new MenuItem(ws.DisplayLabel, mnuAddWS_Anal);
				xmlWsV[i].Tag = ws;
				xmlWsA[i].Tag = ws;
			}
			xmlWsV[xmlWs.Count] = new MenuItem("-");
			xmlWsV[xmlWs.Count + 1] = new MenuItem(LexTextControls.ks_DefineNew_, mnuAddWS_Vern);
			xmlWsA[xmlWs.Count] = new MenuItem("-");
			xmlWsA[xmlWs.Count + 1] = new MenuItem(LexTextControls.ks_DefineNew_, mnuAddWS_Anal);

			// have to have separate lists
			mnuAddWs.MenuItems.Add(LexTextControls.ks_VernacularWS, xmlWsV);
			mnuAddWs.MenuItems.Add(LexTextControls.ks_AnalysisWS, xmlWsA);

			mnuAddWs.Show(this, new Point(0, Height));
		}

		private List<CoreWritingSystemDefinition> GetOtherWritingSystems()
		{
			return m_cache.ServiceLocator.WritingSystemManager.OtherWritingSystems.Where(ws => !m_existingWsIds.Contains(ws.Id)).OrderBy(ws => ws.DisplayLabel).ToList();
		}

		private void mnuAddWS_Vern(object sender, EventArgs e)
		{
			CommonAddWS(false, (sender as MenuItem));
		}

		private void mnuAddWS_Anal(object sender, EventArgs e)
		{
			CommonAddWS(true, (sender as MenuItem));
		}

		private void CommonAddWS(bool isAnalysis, MenuItem selectedMI)
		{
			CoreWritingSystemDefinition ws = null;

			if (selectedMI.Text == LexTextControls.ks_DefineNew_)
			{
				IEnumerable<CoreWritingSystemDefinition> newWritingSystems;
				if (WritingSystemPropertiesDialog.ShowNewDialog(FindForm(), m_cache, m_cache.ServiceLocator.WritingSystemManager,
					m_cache.ServiceLocator.WritingSystems, m_helpTopicProvider, m_app, true, null, out newWritingSystems))
				{
					ws = newWritingSystems.First();
				}
			}
			else
			{
				ws = selectedMI.Tag as CoreWritingSystemDefinition;
			}

			if (ws != null)
			{
				NewWritingSystem = ws;
				// now add the ws to the LCM list for it
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				{
					// Add a global writing system to the local writing system store.  (Replace
					// does this if there's nothing to replace.)
					if (NewWritingSystem.Handle == 0)
					{
						m_cache.ServiceLocator.WritingSystemManager.Replace(NewWritingSystem);
					}
					if (isAnalysis)
					{
						m_cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Add(NewWritingSystem);
						if (!m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Contains(NewWritingSystem))
						{
							m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Add(NewWritingSystem);
						}
					}
					else
					{
						m_cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Add(NewWritingSystem);
						if (!m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Contains(NewWritingSystem))
						{
							m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Add(NewWritingSystem);
						}
					}
					ProgressDialogWithTask.ImportTranslatedListsForWs(this.FindForm(), m_cache, NewWritingSystem.IcuLocale);
				});
				WritingSystemAdded?.Invoke(this, new EventArgs());
			}
		}

		/// <summary>
		/// Get the new writing system added by clicking this button and following the popup menus.
		/// </summary>
		public CoreWritingSystemDefinition NewWritingSystem { get; private set; }
	}
}
