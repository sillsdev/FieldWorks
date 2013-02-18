using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;
using System;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class is instantiated by reflection, based on the setting of the treeBarHandler in the
	/// SemanticDomainList clerk in the RDE toolConfiguration.xml.
	/// </summary>
	class SemanticDomainRdeTreeBarHandler : PossibilityTreeBarHandler
	{
		private PaneBar m_titleBar;
		private Panel m_headerPanel;
		private FwTextBox m_textSearch;
		private FwCancelSearchButton m_btnCancelSearch;
		private SearchTimer m_searchTimer;
		private TreeView m_treeView;
		private ListView m_listView;
		private IVwStylesheet m_stylesheet;
		private ICmSemanticDomainRepository m_semDomRepo;

		/// <summary>
		/// Need a constructor with no parameters for use with DynamicLoader
		/// </summary>
		public SemanticDomainRdeTreeBarHandler()
		{
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="treeBarControl is a reference")]
		internal override void Init(Mediator mediator, XmlNode node)
		{
			base.Init(mediator, node);

			m_semDomRepo = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
			m_stylesheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
			var treeBarControl = GetTreeBarControl(mediator);
			SetupAndShowHeaderPanel(mediator, node, treeBarControl);
			m_searchTimer = new SearchTimer(treeBarControl, 500, HandleChangeInSearchText,
				new List<Control> { treeBarControl.TreeView, treeBarControl.ListView });
			m_textSearch.TextChanged += m_searchTimer.OnSearchTextChanged;
			m_treeView = treeBarControl.TreeView;
			m_listView = treeBarControl.ListView;
			m_listView.HeaderStyle = ColumnHeaderStyle.None; // We don't want a secondary "Records" title bar
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="PaneBar and Panel get added to controls collection and disposed there")]
		private void SetupAndShowHeaderPanel(Mediator mediator, XmlNode node, RecordBar treeBarControl)
		{
			if (!treeBarControl.HasHeaderControl)
			{
				m_titleBar = new PaneBar { Dock = DockStyle.Top };
				var headerPanel = new Panel { Visible = false };
				headerPanel.Controls.Add(m_titleBar);
				m_btnCancelSearch = new FwCancelSearchButton();
				m_btnCancelSearch.Init();
				m_btnCancelSearch.Click += new EventHandler(m_btnCancelSearch_Click);
				headerPanel.Controls.Add(m_btnCancelSearch);
				m_textSearch = CreateSearchBox();
				m_textSearch.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
				headerPanel.Controls.Add(m_textSearch);
				m_textSearch.AdjustForStyleSheet(m_stylesheet);
				headerPanel.Height = SetHeaderPanelHeight();
				treeBarControl.AddHeaderControl(headerPanel);
				// Keep the text box from covering the cancel search button
				m_textSearch.Width = headerPanel.Width - m_btnCancelSearch.Width;
				m_btnCancelSearch.Location = new Point(headerPanel.Width - m_btnCancelSearch.Width, m_textSearch.Location.Y);
				SetInfoBarText(node, m_titleBar);
			}
			treeBarControl.ShowHeaderControl();
		}

		private FwTextBox CreateSearchBox()
		{
			var searchBox = new FwTextBox();
			searchBox.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			searchBox.WritingSystemCode = m_cache.DefaultAnalWs;
			searchBox.Location = new Point(0, 25);
			searchBox.Size = new Size(305, 20);
			searchBox.AdjustStringHeight = true;
			searchBox.HasBorder = false;
			searchBox.BorderStyle = BorderStyle.Fixed3D;
			searchBox.SuppressEnter = true;
			searchBox.Enabled = true;
			searchBox.GotFocus += new EventHandler(m_textSearch_GotFocus);
			return searchBox;
		}

		private void m_btnCancelSearch_Click(object sender, EventArgs e)
		{
			m_textSearch.Text = "";
		}

		private void HandleChangeInSearchText()
		{
			var searchString = TrimSearchTextHandleEnterSpecialCase();
			SearchSemanticDomains(searchString);
		}

		private string TrimSearchTextHandleEnterSpecialCase()
		{
			var searchString = m_textSearch.Tss.Text ?? string.Empty;
			// if string is only whitespace (especially <Enter>), reset to avoid spurious searches with no results.
			if (!string.IsNullOrEmpty(searchString) && string.IsNullOrWhiteSpace(searchString))
			{
				searchString = string.Empty;
				// We must be careful about setting the textbox string, because each time you set it
				// triggers a new search timer iteration. We want to avoid a "continual" reset.
				// We could just ignore whitespace, but if <Enter> gets in there, somehow it makes the
				// rest of the string invisible on the screen. So this special case is handled by resetting
				// the search string to empty if it only contains whitespace.
				m_textSearch.Tss = m_cache.TsStrFactory.MakeString(string.Empty, m_cache.DefaultAnalWs);
			}
			return searchString.Trim();
		}

		private void SearchSemanticDomains(string searchString)
		{
			// The FindDomainsThatMatch method returns IEnumerable<ICmSemanticDomain>
			// based on the search string we give it.
			m_textSearch.Update();
			if (!string.IsNullOrEmpty(searchString))
			{
				try
				{
					m_listView.ItemChecked -= OnDomainListChecked;
					var semDomainsToShow = m_semDomRepo.FindDomainsThatMatch(searchString);
					SemanticDomainSelectionUtility.UpdateDomainListLabels(
						ObjectLabel.CreateObjectLabels(m_cache, semDomainsToShow, string.Empty,
							m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(m_cache.DefaultAnalWs)),
						m_stylesheet, m_listView, true);
					m_treeView.Visible = false;
					m_listView.Visible = true;
					m_btnCancelSearch.SearchIsActive = true;
				}
				finally
				{
					m_listView.ItemChecked += OnDomainListChecked;
				}
			}
			else
			{
				m_treeView.Visible = true;
				m_listView.Visible = false;
				m_btnCancelSearch.SearchIsActive = false;
			}
		}

		private void OnDomainListChecked(object sender, ItemCheckedEventArgs e)
		{
			var domain = m_semDomRepo.GetObject((int) e.Item.Tag);
			SemanticDomainSelectionUtility.AdjustSelectedDomainList(domain, m_stylesheet, e.Item.Checked, m_listView);
		}

		private void m_textSearch_GotFocus(object sender, EventArgs e)
		{
			m_textSearch.SelectAll();
		}

		private int SetHeaderPanelHeight()
		{
			return m_textSearch.Height + m_titleBar.Height;
		}

		private RecordBar GetTreeBarControl(Mediator mediator)
		{
			var window = (XWindow)mediator.PropertyTable.GetValue("window");
			return window.TreeBarControl;
		}

		private void SetInfoBarText(XmlNode handlerNode, PaneBar infoBar)
		{
			var stringTable = m_mediator.StringTbl;

			var titleStr = string.Empty;
			// See if we have an AlternativeTitle string table id for an alternate title.
			var titleId = XmlUtils.GetAttributeValue(handlerNode, "altTitleId");
			if (titleId != null)
			{
				XmlViewsUtils.TryFindString(stringTable, "AlternativeTitles", titleId, out titleStr);
				// if they specified an altTitleId, but it wasn't found, they need to do something,
				// so just return *titleId*
				if (titleStr == null)
					titleStr = titleId;
			}
			infoBar.Text = titleStr;
		}

		/// <summary>
		/// If we are controlling the RecordBar, we want the optional info bar visible.
		/// </summary>
		protected override void UpdateHeaderVisibility()
		{
			var window = (XWindow)m_mediator.PropertyTable.GetValue("window");
			if (window == null || window.IsDisposed)
				return;

			if (IsShowing)
			{
				window.TreeBarControl.ShowHeaderControl();
				HandleChangeInSearchText(); // in case we already have a search active when we enter
			}
		}

		/// <summary>
		/// A trivial override to use a special method to get the names of items.
		/// For semantic domain in this tool we want to display a sense count (if non-zero).
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="font"></param>
		/// <returns></returns>
		protected override string GetTreeNodeLabel(ICmObject obj, out Font font)
		{
			var baseName = base.GetTreeNodeLabel(obj, out font);
			var sd = obj as ICmSemanticDomain;
			if (sd == null)
				return baseName; // pathological defensive programming
			int senseCount = SemanticDomainSelectionUtility.SenseReferenceCount(sd);
			if (senseCount == 0)
				return baseName;
			return baseName + " (" + senseCount + ")";
		}
	}
}
