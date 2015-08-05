using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
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

		public override void SetDlgInfo(FdoCache cache, WindowParams wp, Mediator mediator, IPropertyTable propertyTable)
		{
			SetDlgInfo(cache, wp, mediator, propertyTable, cache.DefaultAnalWs);
		}

		public override void SetDlgInfo(FdoCache cache, WindowParams wp, Mediator mediator, IPropertyTable propertyTable, string form)
		{
			SetDlgInfo(cache, wp, mediator, propertyTable, form, cache.DefaultAnalWs);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "searchEngine is disposed by the mediator.")]
		protected override void InitializeMatchingObjects(FdoCache cache)
		{
			var xnWindow = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
			XmlNode configNode = xnWindow.SelectSingleNode("controls/parameters/guicontrol[@id=\"matchingRecords\"]/parameters");

			SearchEngine searchEngine = SearchEngine.Get(m_mediator, m_propertyTable, "RecordGoSearchEngine", () => new RecordGoSearchEngine(cache));

			m_matchingObjectsBrowser.Initialize(cache, FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable), m_mediator, m_propertyTable, configNode,
				searchEngine);

			// start building index
			var ws = (IWritingSystem) m_cbWritingSystems.SelectedItem;
			if (ws != null)
			{
				ITsString tss = m_tsf.MakeString(string.Empty, ws.Handle);
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

			var ws = (IWritingSystem) m_cbWritingSystems.SelectedItem;
			int wsSelHvo = ws != null ? ws.Handle : 0;
			if (wsSelHvo == 0)
			{
				wsSelHvo = TsStringUtils.GetWsAtOffset(m_tbForm.Tss, 0);
				if (wsSelHvo == 0)
					return;
			}

			if (m_oldSearchKey != string.Empty || searchKey != string.Empty)
				StartSearchAnimation();

			ITsString tss = m_tsf.MakeString(searchKey, wsSelHvo);
			var field = new SearchField(RnGenericRecTags.kflidTitle, tss);
			m_matchingObjectsBrowser.SearchAsync(new[] { field });
		}

		protected override void m_btnInsert_Click(object sender, EventArgs e)
		{
			using (var dlg = new InsertRecordDlg())
			{
				string title = m_tbForm.Text.Trim();
				ITsString titleTrimmed = TsStringUtils.MakeTss(title, TsStringUtils.GetWsAtOffset(m_tbForm.Tss, 0));
				dlg.SetDlgInfo(m_cache, m_mediator, m_propertyTable, m_cache.LanguageProject.ResearchNotebookOA, titleTrimmed);
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
