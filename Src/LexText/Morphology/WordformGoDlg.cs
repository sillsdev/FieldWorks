using System.Collections.Generic;
using System.Linq;
using System.Xml;

using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using XCore;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Common.Controls;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Summary description for WordformGoDlg.
	/// </summary>
	public class WordformGoDlg : BaseGoDlg
	{
		#region	Data members

		protected int m_oldSearchWs;

		#endregion

		#region Construction, Initialization, and Disposal

		public WordformGoDlg()
		{
			SetHelpTopic("khtpFindWordform");
			InitializeComponent();
		}

		/// <summary>
		/// Just load current vernacular
		/// </summary>
		protected override void LoadWritingSystemCombo()
		{
			foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
				m_cbWritingSystems.Items.Add(ws);
		}

		#endregion Construction, Initialization, and Disposal

		#region Other methods

		protected override void InitializeMatchingObjects(FdoCache cache, Mediator mediator)
		{
			var xnWindow = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
			var configNode = xnWindow.SelectSingleNode("controls/parameters/guicontrol[@id=\"WordformsBrowseView\"]/parameters");
			m_matchingObjectsBrowser.Initialize(cache, FontHeightAdjuster.StyleSheetFromMediator(mediator), mediator, configNode,
				cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances().Cast<ICmObject>(), SearchType.Prefix,
				GetWordformSearchFields);
		}

		private IEnumerable<SearchField> GetWordformSearchFields(ICmObject obj)
		{
			var wf = (IWfiWordform)obj;
			var wsObj = (IWritingSystem) m_cbWritingSystems.SelectedItem;
			var ws = wsObj != null ? wsObj.Handle : m_oldSearchWs;
			if (m_vernHvos.Contains(ws))
			{
				var form = wf.Form.StringOrNull(ws);
				if (form != null && form.Length > 0)
					yield return new SearchField(WfiWordformTags.kflidForm, form);
			}
		}

		/// <summary>
		/// Reset the list of matching items.
		/// </summary>
		/// <param name="searchKey"></param>
		protected override void ResetMatches(string searchKey)
		{
			using (new WaitCursor(this))
			{
				var wsObj = (IWritingSystem) m_cbWritingSystems.SelectedItem;
				var wsSelHvo = wsObj != null ? wsObj.Handle : 0;

				string form;
				int vernWs;
				if (!GetSearchKey(wsSelHvo, searchKey, out form, out vernWs))
				{
					var ws = StringUtils.GetWsAtOffset(m_tbForm.Tss, 0);
					if (!GetSearchKey(ws, searchKey, out form, out vernWs))
						return;
					wsSelHvo = ws;
				}

				if (m_oldSearchKey == searchKey && m_oldSearchWs == wsSelHvo)
					return; // Nothing new to do, so skip it.
				if (m_oldSearchWs != wsSelHvo)
				{
					m_matchingObjectsBrowser.Reset();
				}

				// disable Go button until we rebuild our match list.
				m_btnOK.Enabled = false;
				m_oldSearchKey = searchKey;
				m_oldSearchWs = wsSelHvo;

				var fields = new List<SearchField>();
				if (form != null)
				{
					var tssForm = m_tsf.MakeString(form, vernWs);
					fields.Add(new SearchField(WfiWordformTags.kflidForm, tssForm));
				}

				if (!Controls.Contains(m_searchAnimation))
				{
					Controls.Add(m_searchAnimation);
					m_searchAnimation.BringToFront();
				}

				m_matchingObjectsBrowser.Search(fields, null);

				if (Controls.Contains(m_searchAnimation))
					Controls.Remove(m_searchAnimation);
			}
		}

		private bool GetSearchKey(int ws, string searchKey, out string form, out int vernWs)
		{
			form = null;
			vernWs = 0;

			if (m_vernHvos.Contains(ws))
			{
				vernWs = ws;
				form = searchKey;
			}
			else
			{
				return false;
			}

			return true;
		}
		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WordformGoDlg));
			this.m_panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).BeginInit();
			this.SuspendLayout();
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			//
			// m_btnInsert
			//
			resources.ApplyResources(this.m_btnInsert, "m_btnInsert");
			//
			// m_objectsLabel
			//
			resources.ApplyResources(this.m_objectsLabel, "m_objectsLabel");
			//
			// WordformGoDlg
			//
			resources.ApplyResources(this, "$this");
			this.m_helpProvider.SetHelpNavigator(this, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("$this.HelpNavigator"))));
			this.Name = "WordformGoDlg";
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
