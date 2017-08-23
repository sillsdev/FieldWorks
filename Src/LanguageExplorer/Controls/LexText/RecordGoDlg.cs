// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;

namespace LanguageExplorer.Controls.LexText
{
	public class RecordGoDlg : BaseGoDlg
	{
		public RecordGoDlg()
		{
			SetHelpTopic("khtpDataNotebook-FindRecordDlg");
			InitializeComponent();
		}

		protected override string PersistenceLabel
		{
			get { return "RecordGo"; }
		}

		public override void SetDlgInfo(LcmCache cache, WindowParams wp, IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			SetDlgInfo(cache, wp, propertyTable, publisher, subscriber, cache.DefaultAnalWs);
		}

		public override void SetDlgInfo(LcmCache cache, WindowParams wp, IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber, string form)
		{
			SetDlgInfo(cache, wp, propertyTable, publisher, subscriber, form, cache.DefaultAnalWs);
		}

		protected override void InitializeMatchingObjects(LcmCache cache)
		{
			var xnWindow = m_propertyTable.GetValue<XElement>("WindowConfiguration");
			var configNode = xnWindow.XPathSelectElement("controls/parameters/guicontrol[@id=\"matchingRecords\"]/parameters");

			SearchEngine searchEngine = SearchEngine.Get(m_propertyTable, "RecordGoSearchEngine", () => new RecordGoSearchEngine(cache));

			m_matchingObjectsBrowser.Initialize(cache, FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable), m_propertyTable, m_publisher, m_subscriber, configNode,
				searchEngine);

			// start building index
			var ws = (CoreWritingSystemDefinition) m_cbWritingSystems.SelectedItem;
			if (ws != null)
			{
				ITsString tss = TsStringUtils.MakeString(string.Empty, ws.Handle);
				var field = new SearchField(RnGenericRecTags.kflidTitle, tss);
				m_matchingObjectsBrowser.SearchAsync(new[] { field });
			}
		}

		protected override void ResetMatches(string searchKey)
		{
			if (m_oldSearchKey == searchKey)
				return; // Nothing new to do, so skip it.

			// disable Go button until we rebuild our match list.
			m_btnOK.Enabled = false;
			m_oldSearchKey = searchKey;

			var ws = (CoreWritingSystemDefinition) m_cbWritingSystems.SelectedItem;
			int wsSelHvo = ws != null ? ws.Handle : 0;
			if (wsSelHvo == 0)
			{
				wsSelHvo = TsStringUtils.GetWsAtOffset(m_tbForm.Tss, 0);
				if (wsSelHvo == 0)
					return;
			}

			if (m_oldSearchKey != string.Empty || searchKey != string.Empty)
				StartSearchAnimation();

			ITsString tss = TsStringUtils.MakeString(searchKey, wsSelHvo);
			var field = new SearchField(RnGenericRecTags.kflidTitle, tss);
			m_matchingObjectsBrowser.SearchAsync(new[] { field });
		}

		protected override void m_btnInsert_Click(object sender, EventArgs e)
		{
			using (var dlg = new InsertRecordDlg())
			{
				string title = m_tbForm.Text.Trim();
				ITsString titleTrimmed = TsStringUtils.MakeString(title, TsStringUtils.GetWsAtOffset(m_tbForm.Tss, 0));
				dlg.SetDlgInfo(m_cache, m_propertyTable, m_publisher, m_cache.LanguageProject.ResearchNotebookOA, titleTrimmed);
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					m_selObject = dlg.NewRecord;
					HandleMatchingSelectionChanged();
					if (m_btnOK.Enabled)
						m_btnOK.PerformClick();
				}
			}
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RecordGoDlg));
			this.m_panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).BeginInit();
			this.SuspendLayout();
			//
			// m_objectsLabel
			//
			resources.ApplyResources(this.m_objectsLabel, "m_objectsLabel");
			//
			// RecordGoDlg
			//
			resources.ApplyResources(this, "$this");
			this.m_helpProvider.SetHelpNavigator(this, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("$this.HelpNavigator"))));
			this.Name = "RecordGoDlg";
			this.m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.m_panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
