// Copyright (c) 2012-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs.Controls;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Areas.Lexicon
{
	/// <summary>
	/// This class is instantiated by reflection, based on the setting of the treeBarHandler in the
	/// SemanticDomainList record list in the RDE toolConfiguration.xml, but is also used to display the Semantic Domain List in the List Edit tool.
	/// </summary>
	internal sealed class SemanticDomainRdeTreeBarHandler : PossibilityTreeBarHandler, ISemanticDomainTreeBarHandler
	{
		private IPaneBar m_titleBar;
		private Panel m_headerPanel;
		private FwTextBox m_textSearch;
		private FwCancelSearchButton m_btnCancelSearch;
		private SearchTimer m_searchTimer;
		private IRecordBar _recordBar;
		private IVwStylesheet m_stylesheet;
		private ICmSemanticDomainRepository m_semDomRepo;

		/// <summary />
		public SemanticDomainRdeTreeBarHandler(IPropertyTable propertyTable)
			: base(propertyTable, false, true, true, "best analorvern")
		{
			m_semDomRepo = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
			m_stylesheet = FwUtils.StyleSheetFromPropertyTable(m_propertyTable);
		}

		#region ISemanticDomainTreeBarHandler implementation

		void ISemanticDomainTreeBarHandler.FinishInitialization(IPaneBar paneBar)
		{
			m_titleBar = paneBar;
			_recordBar = m_propertyTable.GetValue<IFwMainWnd>(FwUtils.window).RecordBarControl;
			if (_recordBar == null)
			{
				return;
			}
			if (!_recordBar.HasHeaderControl)
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
				_recordBar.AddHeaderControl(headerPanel);
				// Keep the text box from covering the cancel search button
				m_textSearch.Width = headerPanel.Width - m_btnCancelSearch.Width;
				m_btnCancelSearch.Location = new Point(headerPanel.Width - m_btnCancelSearch.Width, m_textSearch.Location.Y);
				SetInfoBarText();
			}
			_recordBar.ShowHeaderControl = true;
			m_searchTimer = new SearchTimer((Control)_recordBar, 500, HandleChangeInSearchText, new List<Control> { _recordBar.TreeView, _recordBar.ListView });
			m_textSearch.TextChanged += m_searchTimer.OnSearchTextChanged;
			_recordBar.ListView.HeaderStyle = ColumnHeaderStyle.None; // We don't want a secondary "Records" title bar
		}

		#region Overrides of TreeBarHandler

		public override void PopulateRecordBar(IRecordList recordList)
		{
			PopulateRecordBar(recordList, Editable);
		}

		#endregion

		#endregion ISemanticDomainTreeBarHandler implementation

		// Semantic Domains should be editable only in the Lists area.
		private bool Editable => AreaServices.ListsAreaMachineName.Equals(m_propertyTable.GetValue<string>(AreaServices.AreaChoice));

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
			m_textSearch.Text = string.Empty;
		}

		private void HandleChangeInSearchText()
		{
			SearchSemanticDomains(TrimSearchTextHandleEnterSpecialCase);
		}

		private string TrimSearchTextHandleEnterSpecialCase
		{
			get
			{
				var searchString = m_textSearch.Tss.Text ?? string.Empty;
				// if string is only whitespace (especially <Enter>), reset to avoid spurious searches with no results.
				if (string.IsNullOrWhiteSpace(searchString))
				{
					// We must be careful about setting the textbox string, because each time you set it
					// triggers a new search timer iteration. We want to avoid a "continual" reset.
					// We could just ignore whitespace, but if <Enter> gets in there, somehow it makes the
					// rest of the string invisible on the screen. So this special case is handled by resetting
					// the search string to empty if it only contains whitespace.
					m_textSearch.Tss = TsStringUtils.EmptyString(m_cache.DefaultAnalWs);
					return string.Empty;
				}
				return searchString.Trim();
			}
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
					_recordBar.IsFlatList = true;
					_recordBar.ListView.ItemChecked -= OnDomainListChecked;
					var semDomainsToShow = m_semDomRepo.FindDomainsThatMatch(searchString);
					SemanticDomainSelectionServices.UpdateDomainListLabels(
						ObjectLabel.CreateObjectLabels(m_cache, semDomainsToShow, string.Empty, m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(m_cache.DefaultAnalWs)),
						m_stylesheet, _recordBar.ListView, true);
					m_btnCancelSearch.SearchIsActive = true;
				}
				finally
				{
					_recordBar.ListView.ItemChecked += OnDomainListChecked;
				}
			}
			else
			{
				_recordBar.IsFlatList = false;
				m_btnCancelSearch.SearchIsActive = false;
			}
		}

		private void OnDomainListChecked(object sender, ItemCheckedEventArgs e)
		{
			SemanticDomainSelectionServices.AdjustSelectedDomainList(m_semDomRepo.GetObject((int)e.Item.Tag), m_stylesheet, e.Item.Checked, _recordBar.ListView);
		}

		private void m_textSearch_GotFocus(object sender, EventArgs e)
		{
			m_textSearch.SelectAll();
		}

		private int SetHeaderPanelHeight()
		{
			return m_textSearch.Height + ((Control)m_titleBar).Height;
		}

		private void SetInfoBarText()
		{
			const string titleId = "SemanticDomain-Plural";
			string titleStr;
			XmlViewsUtils.TryFindString(StringTable.AlternativeTitles, titleId, out titleStr);
			// if they specified an altTitleId, but it wasn't found, they need to do something,
			// so just return *titleId*
			if (titleStr == null)
			{
				titleStr = titleId;
			}
			m_titleBar.Text = titleStr;
		}

		/// <summary>
		/// If we are controlling the RecordBar, we want the optional info bar visible.
		/// </summary>
		protected override void UpdateHeaderVisibility()
		{
			var window = m_propertyTable.GetValue<IFwMainWnd>(FwUtils.window);
			if (window == null)
			{
				return;
			}
			window.RecordBarControl.ShowHeaderControl = true;
			HandleChangeInSearchText(); // in case we already have a search active when we enter
		}

		/// <summary>
		/// A trivial override to use a special method to get the names of items.
		/// For semantic domain in this tool we want to display a sense count (if non-zero).
		/// </summary>
		protected override string GetTreeNodeLabel(ICmObject obj, out Font font)
		{
			var baseName = base.GetTreeNodeLabel(obj, out font);
			var sd = obj as ICmSemanticDomain;
			if (sd == null)
			{
				return baseName; // pathological defensive programming
			}
			var senseCount = SemanticDomainSelectionServices.SenseReferenceCount(sd);
			return senseCount == 0 ? baseName : $"{baseName} ({senseCount})";
		}
	}
}