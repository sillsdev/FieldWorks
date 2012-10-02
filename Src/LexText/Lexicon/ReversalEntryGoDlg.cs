using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	///
	/// </summary>
	public class ReversalEntryGoDlg : BaseGoDlg
	{
		private IReversalIndex m_reveralIndex;
		private readonly HashSet<IReversalIndexEntry> m_filteredReversalEntries = new HashSet<IReversalIndexEntry>();

		public ReversalEntryGoDlg()
		{
			SetHelpTopic("khtpFindReversalEntry");
			InitializeComponent();
		}

		/// <summary>
		/// Gets or sets the reversal index.
		/// </summary>
		/// <value>The reversal index.</value>
		public IReversalIndex ReversalIndex
		{
			get
			{
				CheckDisposed();
				return m_reveralIndex;
			}

			set
			{
				CheckDisposed();
				m_reveralIndex = value;
			}
		}

		public ICollection<IReversalIndexEntry> FilteredReversalEntries
		{
			get
			{
				CheckDisposed();
				return m_filteredReversalEntries;
			}
		}

		protected override string PersistenceLabel
		{
			get { return "ReversalEntryGo"; }
		}

		protected override void InitializeMatchingObjects(FdoCache cache, Mediator mediator)
		{
			var xnWindow = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
			XmlNode configNode = xnWindow.SelectSingleNode("controls/parameters/guicontrol[@id=\"matchingReversalEntries\"]/parameters");
			m_matchingObjectsBrowser.Initialize(cache, FontHeightAdjuster.StyleSheetFromMediator(mediator), mediator, configNode,
				cache.ServiceLocator.GetInstance<IReversalIndexEntryRepository>().AllInstances().Cast<ICmObject>(), SearchType.Prefix,
				GetReversalEntrySearchFields, m_cache.ServiceLocator.WritingSystemManager.Get(m_reveralIndex.WritingSystem));
		}

		protected override void LoadWritingSystemCombo()
		{
			m_cbWritingSystems.Items.Add(m_cache.ServiceLocator.WritingSystemManager.Get(m_reveralIndex.WritingSystem));
		}

		private IEnumerable<SearchField> GetReversalEntrySearchFields(ICmObject obj)
		{
			var wsObj = (IWritingSystem) m_cbWritingSystems.SelectedItem;
			if (wsObj != null)
			{
				var rie = (IReversalIndexEntry)obj;
				var form = rie.ReversalForm.StringOrNull(wsObj.Handle);
				if (form != null && form.Length > 0)
					yield return new SearchField(ReversalIndexEntryTags.kflidReversalForm, form);
			}
		}

		public override void SetDlgInfo(FdoCache cache, WindowParams wp, Mediator mediator)
		{
			SetDlgInfo(cache, wp, mediator, cache.ServiceLocator.WritingSystemManager.GetWsFromStr(m_reveralIndex.WritingSystem));
		}

		public override void SetDlgInfo(FdoCache cache, WindowParams wp, Mediator mediator, string form)
		{
			SetDlgInfo(cache, wp, mediator, form, cache.ServiceLocator.WritingSystemManager.GetWsFromStr(m_reveralIndex.WritingSystem));
		}

		protected override void ResetMatches(string searchKey)
		{
			using (new WaitCursor(this))
			{
				if (m_oldSearchKey == searchKey)
					return; // Nothing new to do, so skip it.

				// disable Go button until we rebuild our match list.
				m_btnOK.Enabled = false;
				m_oldSearchKey = searchKey;

				var wsObj = (IWritingSystem)m_cbWritingSystems.SelectedItem;
				if (wsObj == null)
					return;

				ITsString tss = m_tsf.MakeString(searchKey, wsObj.Handle);
				var field = new SearchField(ReversalIndexEntryTags.kflidReversalForm, tss);

				if (!Controls.Contains(m_searchAnimation))
				{
					Controls.Add(m_searchAnimation);
					m_searchAnimation.BringToFront();
				}

				m_matchingObjectsBrowser.Search(new[] { field }, m_filteredReversalEntries.Cast<ICmObject>());

				if (Controls.Contains(m_searchAnimation))
					Controls.Remove(m_searchAnimation);
			}
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReversalEntryGoDlg));
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
			// m_cbWritingSystems
			//
			resources.ApplyResources(this.m_cbWritingSystems, "m_cbWritingSystems");
			//
			// m_objectsLabel
			//
			resources.ApplyResources(this.m_objectsLabel, "m_objectsLabel");
			//
			// ReversalEntryGoDlg
			//
			resources.ApplyResources(this, "$this");
			this.m_helpProvider.SetHelpNavigator(this, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("$this.HelpNavigator"))));
			this.Name = "ReversalEntryGoDlg";
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
