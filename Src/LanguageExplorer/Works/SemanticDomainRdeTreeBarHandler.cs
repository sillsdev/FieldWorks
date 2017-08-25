// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace LanguageExplorer.Works
{
	/// <summary>
	/// This class is instantiated by reflection, based on the setting of the treeBarHandler in the
	/// SemanticDomainList clerk in the RDE toolConfiguration.xml, but is also used to display the Semantic Domain List in the List Edit tool.
	/// </summary>
	class SemanticDomainRdeTreeBarHandler : PossibilityTreeBarHandler
	{
		private IPaneBar m_titleBar;
		private Panel m_headerPanel;
		private FwTextBox m_textSearch;
		private FwCancelSearchButton m_btnCancelSearch;
		private SearchTimer m_searchTimer;
		private TreeView m_treeView;
		private ListView m_listView;
		private IVwStylesheet m_stylesheet;
		private ICmSemanticDomainRepository m_semDomRepo;
		private XElement m_configurationParametersElement;

		/// <summary />
		public SemanticDomainRdeTreeBarHandler(IPropertyTable propertyTable, XElement configurationParametersElement)
			: base(propertyTable, bool.Parse(configurationParametersElement.Attribute("expand").Value), bool.Parse(configurationParametersElement.Attribute("hierarchical").Value), bool.Parse(configurationParametersElement.Attribute("includeAbbr").Value), configurationParametersElement.Attribute("ws").Value)
		{
			m_configurationParametersElement = configurationParametersElement;

			m_semDomRepo = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
			m_stylesheet = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable);
		}

		internal void FinishInitialization(IPaneBar paneBar)
		{
			m_titleBar = paneBar;
			var treeBarControl = GetTreeBarControl();
			if (treeBarControl == null)
				return;
			SetupAndShowHeaderPanel(treeBarControl);
			m_searchTimer = new SearchTimer((Control)treeBarControl, 500, HandleChangeInSearchText,
				new List<Control> { treeBarControl.TreeView, treeBarControl.ListView });
			m_textSearch.TextChanged += m_searchTimer.OnSearchTextChanged;
			m_treeView = treeBarControl.TreeView;
			m_listView = treeBarControl.ListView;
			m_listView.HeaderStyle = ColumnHeaderStyle.None; // We don't want a secondary "Records" title bar
		}

		private void SetupAndShowHeaderPanel(IRecordBar treeBarControl)
		{
			if (!treeBarControl.HasHeaderControl)
			{
				var headerPanel = new Panel { Visible = false };
				headerPanel.Controls.Add((Control)m_titleBar);
				m_btnCancelSearch = new FwCancelSearchButton();
				m_btnCancelSearch.Init();
				m_btnCancelSearch.Click += m_btnCancelSearch_Click;
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
				SetInfoBarText();
			}
			treeBarControl.ShowHeaderControl = true;
		}

		public override void PopulateRecordBar(RecordList list)
		{
			PopulateRecordBar(list, Editable);
		}

		// Semantic Domains should be editable only in the Lists area.
		protected bool Editable { get { return "lists".Equals(m_propertyTable.GetValue<string>("areaChoice")); } }

		private FwTextBox CreateSearchBox()
		{
			var searchBox = new FwTextBox
			{
				WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor,
				WritingSystemCode = m_cache.DefaultAnalWs,
				Location = new Point(0, 25),
				Size = new Size(305, 20),
				AdjustStringHeight = true,
				HasBorder = false,
				BorderStyle = BorderStyle.Fixed3D,
				SuppressEnter = true,
				Enabled = true
			};
			searchBox.GotFocus += m_textSearch_GotFocus;
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
				m_textSearch.Tss = TsStringUtils.EmptyString(m_cache.DefaultAnalWs);
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
			return m_textSearch.Height + ((Control)m_titleBar).Height;
		}

		private IRecordBar GetTreeBarControl()
		{
			var window = m_propertyTable.GetValue<IFwMainWnd>("window");
			return window.RecordBarControl;
		}

		private void SetInfoBarText()
		{
			var titleStr = string.Empty;
			// See if we have an AlternativeTitle string table id for an alternate title.
			var titleId = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "altTitleId");
			if (titleId != null)
			{
				XmlViewsUtils.TryFindString("AlternativeTitles", titleId, out titleStr);
				// if they specified an altTitleId, but it wasn't found, they need to do something,
				// so just return *titleId*
				if (titleStr == null)
					titleStr = titleId;
			}
			m_titleBar.Text = titleStr;
		}

		/// <summary>
		/// If we are controlling the RecordBar, we want the optional info bar visible.
		/// </summary>
		protected override void UpdateHeaderVisibility()
		{
			var window = m_propertyTable.GetValue<IFwMainWnd>("window");
			if (window == null)
				return;

			if (IsShowing)
			{
				window.RecordBarControl.ShowHeaderControl = true;
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
